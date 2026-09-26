"""ChatGPT's UnitySkills client for art iterations (tooling, not a game asset).

Purpose: drives the open Unity editor over the UnitySkills local REST API (ports 8090-8100,
agent id Codex-ArtIteration): state(), stop(), start() (enters play mode and presses the
morning briefing's Start Shift button, OfficeOverlayCanvas/BriefingPanel/Paper/ActionButton,
which the gameplay layer builds; the art scene's leftover copy is switched off at load),
call(skill, **args), batch(skill, items), save() (OfficeScene) and capture(label).
Inputs: a running editor with UnitySkills on this project. Importing it finds the server.
Outputs: capture(label) copies the Game-view screenshot from Assets/Screenshots to
{label}.png in this folder.
Run: import from Python (HANDOVER_2026-09-25.md): sys.path.insert(0, 'ArtDeliverables/
TimeDesk/DeskFinish/Iterations'); from iteration_api import *
"""
import json,time,urllib.request,concurrent.futures
from pathlib import Path
HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[3]
HEADERS={'Content-Type':'application/json','X-Agent-Id':'Codex-ArtIteration'}
def discover():
 def probe(port):
  try:
   base=f'http://127.0.0.1:{port}'
   health=json.load(urllib.request.urlopen(base+'/health',timeout=4))
   if health.get('projectName')=='NOPE' and health.get('serverRunning'):return base
  except Exception:pass
 with concurrent.futures.ThreadPoolExecutor(max_workers=11) as executor:
  for base in executor.map(probe,range(8090,8101)):
   if base:return base
 raise RuntimeError('No responsive NOPE UnitySkills server')
for _boot in range(4):
 try:
  BASE=discover();break
 except RuntimeError:
  if _boot==3:raise
  time.sleep(.5)
def call(skill,**args):
 req=urllib.request.Request(BASE+'/skill/'+skill,data=json.dumps(args).encode(),headers=HEADERS)
 raw=json.load(urllib.request.urlopen(req,timeout=60))
 if raw.get('status')!='success':raise RuntimeError(raw)
 result=raw.get('result',{})
 if result.get('success') is False or result.get('failCount',0):raise RuntimeError(result)
 return result
def batch(skill,items):return call(skill,items=json.dumps(items))
def state():
 global BASE
 for attempt in range(8):
  try:
   BASE=discover()
   return call('editor_get_state')
  except (json.JSONDecodeError,urllib.error.URLError,RuntimeError):time.sleep(.4)
 raise RuntimeError('Editor state unavailable after reload')
def stop():
 if state()['isPlaying']:call('editor_stop')
 for _ in range(30):
  if not state()['isPlaying']:return
  time.sleep(.25)
 raise RuntimeError('Editor did not leave Play mode')
def start():
 if not state()['isPlaying']:call('editor_play')
 for _ in range(30):
  if state()['isPlaying']:break
  time.sleep(.25)
 time.sleep(.5)
 call('event_invoke',path='OfficeOverlayCanvas/BriefingPanel/Paper/ActionButton',componentName='Button',eventName='onClick')
def capture(label):
 filename='desk_'+label+'.png'
 r=call('scene_screenshot',filename=filename,width=1920,height=1080)
 src=PROJECT/'Assets/Screenshots'/filename
 for _ in range(30):
  if src.exists():
   time.sleep(.15)
   dest=HERE/(label+'.png');dest.write_bytes(src.read_bytes());print(dest,flush=True);return dest
  time.sleep(.2)
 raise RuntimeError('Screenshot was not written')
def save():return call('scene_save',scenePath='Assets/Scenes/OfficeScene.unity')
