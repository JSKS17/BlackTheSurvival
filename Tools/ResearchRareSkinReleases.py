"""Cache official notices relevant to the candidate rare-encounter skin list."""
import concurrent.futures
import html
import json
import re
import urllib.parse
import urllib.request
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DOC=ROOT/'docs/art-rare-skins'
CACHE=ROOT/'Temp/RareSkinNotices'


def fetch(url,path):
    if path.exists(): return path.read_text(encoding='utf8')
    request=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
    with urllib.request.urlopen(request,timeout=30) as response:
        text=response.read().decode('utf8')
    path.write_text(text,encoding='utf8')
    return text


def plain(text):
    text=re.sub(r'<script\b[^>]*>.*?</script>','',text,flags=re.S)
    text=re.sub(r'<style\b[^>]*>.*?</style>','',text,flags=re.S)
    text=re.sub(r'</(?:p|div|h[1-6]|li|tr)>','\n',text)
    text=html.unescape(re.sub(r'<[^>]+>',' ',text))
    return re.sub(r'\s+',' ',text)


def research(entry):
    sid=entry['id']
    name=entry['skinNameKR']
    # Skin registry writes "니아 (물속성)" while notices use "(물속성) 니아".
    query={'nia':'물속성','dailin':'잭 오','hart':'3rd Anniversary 하트','rio':'3rd Anniversary 리오','sua':'1st Anniversary','eva':'2nd Anniversary','johann':'악마사냥꾼 요한','ian':'이끌린 이안','debi_marlene':'방과 후 자유시간','coraline':'설램 가득'}.get(sid,name)
    url='https://playeternalreturn.com/api/v1/posts/news?'+urllib.parse.urlencode(dict(hl='ko-KR',search_type='content',search_text=query))
    data=json.loads(fetch(url,CACHE/(sid+'-search.json')))
    articles=data['articles']
    for page in range(2,data.get('total_page',1)+1):
        articles.extend(json.loads(fetch(url+'&page='+str(page),CACHE/(sid+f'-search-{page}.json')))['articles'])
    useful=[]
    for article in articles:
        info=article['i18ns'].get('ko_KR',{})
        title=info.get('title','')
        if (article['category_id']==28 or name in title or '랭크 보상' in title or '스킨' in title) and ('확률' not in title):
            useful.append(article)
    useful=sorted(useful,key=lambda a:a['created_at'])
    # Earliest release patch and previews; later bug-fix mentions are not release proof.
    selected=useful[:5]
    snippets=[]
    for article in selected:
        aid=article['id']
        fullurl=article['url']+'?hl=ko-KR'
        body=plain(fetch(fullurl,CACHE/(str(aid)+'.html')))
        matches=list(re.finditer(re.escape(query),body,re.I))
        contexts=[body[max(0,m.start()-180):m.end()+500].strip() for m in matches[:6]]
        snippets.append(dict(id=aid,url=fullurl,title=article['i18ns'].get('ko_KR',{}).get('title',''),publishedAt=article['created_at'],contexts=contexts))
    return dict(id=sid,name=entry['name'],skinNameKR=name,searchURL=url,totalArticles=len(articles),notices=snippets)


def main():
    CACHE.mkdir(parents=True,exist_ok=True)
    entries=json.loads((DOC/'skin-manifest.json').read_text(encoding='utf8'))['skins']
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        results=list(pool.map(research,[e for e in entries if not e['customOutfit']]))
    (CACHE/'release-research.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    lines=[]
    for item in results:
        lines += ['## '+item['id']+' '+item['skinNameKR']+' ('+str(item['totalArticles'])+' notices)']
        for notice in item['notices']:
            lines += [notice['title']+' | '+notice['url']+' | '+notice['publishedAt']]+notice['contexts']
    (CACHE/'release-research.txt').write_text('\n\n'.join(lines),encoding='utf8')
    print('Cached official notice evidence for '+str(len(results))+' skin candidates.')


if __name__=='__main__': main()
