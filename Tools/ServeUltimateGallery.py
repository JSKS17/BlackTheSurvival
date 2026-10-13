from functools import partial
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import argparse
parser=argparse.ArgumentParser()
parser.add_argument('--port',type=int,default=0)
args=parser.parse_args()
folder=Path(__file__).resolve().parents[1]/'ArtSource/AnimationPrototypes'
server=ThreadingHTTPServer(('127.0.0.1',args.port),partial(SimpleHTTPRequestHandler,directory=str(folder)))
print('Gallery: http://127.0.0.1:'+str(server.server_port)+'/index.html',flush=True)
server.serve_forever()
