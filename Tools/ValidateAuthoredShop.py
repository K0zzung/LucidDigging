"""Read-only wiring checks for the user's authored scene panel. Not Unity visual validation."""
from pathlib import Path
import re,json
root=Path(__file__).resolve().parents[1];s=(root/'Assets/Scenes/1655m.unity').read_text(encoding='utf-8')
bs={m[1]:m[0] for m in re.finditer(r'--- !u!\d+ &(\d+)[^\n]*\n[\s\S]*?(?=--- !u!|\Z)',s)}
assert len(bs)==len(re.findall(r'^--- !u!',s,re.M))
# A component owner must be a GameObject, and must register that component.
# A Canvas component fileID here crashes Unity during AwakeFromLoad.
for fid,block in bs.items():
 owner=re.search(r'^  m_GameObject: \{fileID: (\d+)\}',block,re.M)
 if owner and owner[1] in bs:
  gameobject=bs[owner[1]]
  assert gameobject.startswith('--- !u!1 &'),(fid,'owner is not a GameObject')
  if 'stripped' not in gameobject.splitlines()[0]:
   assert f'component: {{fileID: {fid}}}' in gameobject,(fid,'missing m_Component registration')
guid=(root/'Assets/Scripts/UI/ShopPanelController.cs.meta').read_text().split('guid: ')[1].splitlines()[0]
controller=next(b for b in bs.values() if 'guid: '+guid in b)
for fid in re.findall(r'{fileID: (\d+)}',controller):
 if fid!='0':assert fid in bs,fid
for key,length in [('tabButtons',3),('contentRoots',3),('statButtons',2),('menuButtons',2)]:
 block=re.search('  '+key+r':\n((?:  - .*\n)+)',controller)[1];assert len(block.splitlines())==length
assert '  - {fileID: 0}\n  - {fileID: 0}' in controller
assert 'm_IsActive: 0' in bs['661643066'] and 'm_Enabled: 0' in bs['2147000020'] and 'm_Enabled: 0' in bs['1977912181']
assert 'm_AnchoredPosition: {x: 0.4891, y: -5.393}' in bs['588492055']
assert 'm_LocalScale: {x: 100, y: 100, z: 3.3855798}' in bs['588492055']
assert 'm_SizeDelta: {x: 11.7107, y: 20.437}' in bs['588492055']
assert 'm_Enabled: 0' in bs['588492058']
assert 'm_AnchoredPosition: {x: 11, y: -494}' in bs['81891771']
# Currency TMP references must be the user's existing objects.
for name,fid in [('pinkText','1793193185'),('blueText','644958712'),('yellowText','948649252')]:assert f'{name}: {{fileID: {fid}}}' in controller
# Existing X, enhancement button and selection controls retain user-owned event targets.
assert 'closeButton: {fileID: 1811215620}' in controller and 'buyButton: {fileID: 30078425}' in controller
for fid in ['1811215620','30078425']:assert 'm_Calls: []' in bs[fid]
assert 'fileID: -470165588' in bs['1023418391']
assert len(re.findall('  m_Name: ShopPanel\n',s))==1 and len(re.findall('  m_Name: EventSystem\n',s))==1
source=(root/'Assets/Scripts/UI/ShopPanelController.cs').read_text(encoding='utf-8')
assert 'Instantiate(' not in source
assert 'previewCamera: {fileID: 598365417}' in controller
assert 'm_LocalScale: {x: 0.7, y: 0.7, z: 1}' in bs['2147101046']
assert 'playerPreview.texture=previewCamera.targetTexture' in source
assert 'guid: 1344c3c82d62a2a41a3576d8abb8e3ea' in bs['2147101048']
assert 'm_Texture: {fileID: 8400000, guid: 05814f943bc3e2e4581d2a27cf38a862' in bs['2147101048']
assert 'new GameObject(' not in source
assert '.SetUpdate(true)' in source and 'openPosition=panel.anchoredPosition' in source
report={'authored_shop_panel_instances':1,'old_shop_ui_disabled':True,'original_panel_transform_preserved':True,'original_play_position_preserved':True,'original_currency_tmp_reused':True,'equipment_skill_content_roots_unassigned':True,'ui_only_player_preview':True,'new_panel_instantiation':False,'unity_play_mode_tested':False}
(root/'Artifacts/Shop').mkdir(parents=True,exist_ok=True);(root/'Artifacts/Shop/AuthoredPanelWiring.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('PASS: authored panel references, original transforms, existing currency TMPs and disabled old shop. Shared equipped-player camera reference and scale verified.')
