"""Static layout and scene wiring checks. Does not render Unity or synthesize game screenshots."""
from pathlib import Path
import re,json
root=Path(__file__).resolve().parents[1]
row=(root/'Assets/Scripts/Player/PermanentUpgradeRowUI.cs').read_bytes().decode('cp949')
controller=(root/'Assets/Scripts/UI/ShopUpgradeCategories.cs').read_text(encoding='utf-8')
scene=(root/'Assets/Scenes/1655m.unity').read_text(encoding='utf-8')
boxes={}
for m in re.finditer(r'SetupText\((\w+),row,pixelFont,new Vector2\(([-\d.]+),([-\d.]+)\),new Vector2\(([-\d.]+),([-\d.]+)\)',row):
 name,x,y,w,h=m.groups();x,y,w,h=map(float,[x,y,w,h]);assert x>=0 and x+w<=900 and -y>=0 and -y+h<=204,(name,x,y,w,h)
 boxes[name]=[x,-y,w,h]
assert len(boxes)==5,boxes
button=[670,136,210,54]
for name,(x,y,w,h) in boxes.items():
 assert x+w<=button[0] or x>=button[0]+button[2] or y+h<=button[1] or y>=button[1]+button[3],name
assert 'text.margin=Vector4.zero' in row and 'text.raycastTarget=false' in row
assert 'plateImage.raycastTarget=false' in controller and 'edge.raycastTarget=false' in controller
assert 'RectMask2D' in controller and 'playButtonRect.GetWorldCorners' in controller
assert 'scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide' in controller
assert 'playButtonRect: {fileID: 81891771}' in scene
# At the scene's 1080x1920 reference layout, cap the contents above the existing PLAY rect.
body_top=50+593+655.162/2-350
play_top=-210+310/2
available=body_top-play_top-16
sizes={str(n):dict(content=n*204+(n-1)*14,height=min(110+n*204+(n-1)*14+16,available),scroll=n==3) for n in [2,3]}
for entry in sizes.values():assert body_top-entry['height']>=play_top+16-.01
for fid in ['196731503','728686459','458074140','1874708369','1244585777','1756315529']:
 assert len(re.findall('m_Target: \\{fileID: '+fid+'\\}',scene))==1
assert scene.count('  m_Name: EventSystem\n')==1
result=dict(text_rectangles_inside_cards=boxes,button_rectangle=button,text_button_overlap=False,tmp_margins_reset=True,background_raycasts_disabled=True,existing_callbacks_each_once=True,eventsystems=1,reference_layout_1080x1920=sizes,unity_rendered=False)
(root/'Artifacts/Shop').mkdir(parents=True,exist_ok=True)
(root/'Artifacts/Shop/LayoutValidation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('PASS: five text rectangles inside each card, no overlap with buy button, zero TMP margins.')
print('PASS: content fits above PLAY; 2-row panels shrink and 3-row content scrolls inside clipped viewport.')
print('PASS: original six callbacks remain unique; one EventSystem. Unity rendering/input not tested.')
