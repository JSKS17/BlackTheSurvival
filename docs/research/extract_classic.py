import html, json, re
from pathlib import Path

root = Path(__file__).resolve().parent
source = (root / 'classic_1280.html').read_text(encoding='utf-8-sig')
chapters = re.split(r'<h5 class="er-point-title">', source)[1:]
data = []
for chapter in chapters:
    name, body = chapter.split('</h5>', 1)
    body = body.split('<section', 1)[0]
    body = re.sub(r'</(?:p|li|h\d|tr|div)>|<br\s*/?>', '\n', body)
    body = html.unescape(re.sub(r'<[^>]+>', '', body)).replace('\xa0', ' ')
    lines = [re.sub(r'\s+', ' ', v).strip() for v in body.splitlines() if v.strip()]
    sections=[]
    for i,line in enumerate(lines):
        if len(line) < 90 and re.match(r'^(.{1,50}?)\((?:(?:원거리|근접|이렘|고양이|이안|빙의) )?([PQWER])[12]?(?: - [^)]*)?\)(?:[^.]*)$', line):
            sections.append((i,line))
    skills=[]
    for ix,(i,line) in enumerate(sections):
        end=sections[ix+1][0] if ix+1<len(sections) else len(lines)
        section='\n'.join(lines[i+1:end])
        cooldowns=re.findall(r'쿨다운\s*:?\s*([^\n]+)', section)
        key=re.match(r'^(.{1,50}?)\((?:(?:원거리|근접|이렘|고양이|이안|빙의) )?([PQWER])',line).group(2)
        skills.append({'name':line,'key':key,'cooldown':cooldowns, 'body':section})
    data.append({'name':html.unescape(re.sub(r'<[^>]+>', '',name)), 'skills':skills})
(root/'classic_1280.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
for char in data:
    print(char['name']+' | '+' | '.join(s['name']+' ['+';'.join(s['cooldown'])+']' for s in char['skills']))
