"""Targeted final art checks through the existing Unity editor bridge."""
import json
from PIL import Image
from iteration_api import *

stop()
call('asset_refresh')
save()
call('console_clear')
start()
result={'scene':'Assets/Scenes/OfficeScene.unity','glass':[],
        'note':'API event-path check, not a physical pointer test or full gameplay day.'}
for value in [0,100,25]:
    call('component_set_property',path='HybridOffice/Hall/WindowGlass',
         componentType='OfficeWindowGlass',propertyName='GlassTransparency',value=str(value))
    panes={}
    for side in ['Rear','West','East']:
        info=call('component_get_properties',path='HybridOffice/Hall/WindowGlass/'+side,
                  componentType='MeshRenderer')
        enabled=next(p['value'] for p in info['properties'] if p['name']=='enabled')
        assert enabled==('False' if value==100 else 'True'),(value,side,enabled)
        panes[side]=enabled
    result['glass'].append({'transparency':value,'panes':panes})
p=capture('36_iteration_3_final')
preview=Image.open(p);preview.thumbnail((1920,1080));preview.save(HERE/'36_iteration_3_preview.png')
call('event_invoke',path='OfficeRoot/CRTMonitor',componentName='Clickable',eventName='onClick')
time.sleep(.6)
view=call('component_get_properties',path='OfficeRoot',componentType='OfficeViewController')
result['monitorView']=next(p['value'] for p in view['properties'] if p['name']=='Current')
assert result['monitorView']=='MonitorFocus',result['monitorView']
result['shader']=call('shader_check_errors',shaderNameOrPath='TimeSorter/OfficePortal')
assert not result['shader']['hasErrors'],result['shader']
result['errors']=call('console_get_logs',type='Error',limit=40)
assert result['errors']['count']==0,result['errors']
(HERE/'verification_36.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result),flush=True)
stop()
call('component_set_property',path='HybridOffice/Hall/WindowGlass',
     componentType='OfficeWindowGlass',propertyName='GlassTransparency',value='25')
save()
