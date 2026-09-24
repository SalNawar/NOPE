import json,time,urllib.request
from pathlib import Path
HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[3]
HEADERS={'Content-Type':'application/json','X-Agent-Id':'Codex-ArtIteration'}
def call(skill,**args):
 req=urllib.request.Request('http://localhost:8091/skill/'+skill,data=json.dumps(args).encode(),headers=HEADERS)
 raw=json.load(urllib.request.urlopen(req,timeout=60))
 if raw.get('status')!='success':raise RuntimeError(raw)
 result=raw.get('result',{})
 if result.get('success') is False or result.get('failCount',0):raise RuntimeError(result)
 return result
def batch(skill,items):return call(skill,items=json.dumps(items))
def state():
 for attempt in range(8):
  try:return call('editor_get_state')
  except (json.JSONDecodeError,urllib.error.URLError):time.sleep(.4)
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
