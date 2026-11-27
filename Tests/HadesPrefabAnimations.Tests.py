"""Serialized prefab/controller/clip checks; not a Unity import or play test."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
boss = (root / 'Assets/Prefabs/Hades.prefab').read_text()
for prefab, clip, sheet, start, count in [
    ('boss_ball_0', 'hadesBossball', 'boss_ball', 0, 20),
    ('hadesAttackBall', 'hades_attack_ball', 'All_Fire_Bullet_Pixel_16x16_05', 390, 4),
]:
    path = root / f'Assets/Prefabs/{prefab}.prefab'
    text = path.read_text()
    guid = re.search(r'guid: (\w+)', path.with_suffix('.prefab.meta').read_text())[1]
    assert guid in boss, f'{prefab} is not connected to Hades'
    controller_guid = re.search(r'm_Controller: \{fileID: 9100000, guid: (\w+)', text)[1]
    folder = root / 'Assets/Images/Boss'
    controller_meta = next(p for p in folder.glob('*.controller.meta') if controller_guid in p.read_text())
    controller = controller_meta.with_suffix('').read_text()
    clip_guid = re.search(r'guid: (\w+)', (folder / f'{clip}.anim.meta').read_text())[1]
    assert clip_guid in controller and 'm_AnimatorLayers: []' not in controller
    anim = (folder / f'{clip}.anim').read_text()
    assert 'm_LoopTime: 1' in anim
    keys = re.findall(r'value: \{fileID: (-?\d+)', anim)
    meta = (folder / f'{sheet}.png.meta').read_text()
    entries = {int(m[1].rsplit('_', 1)[1]): m[2] for m in re.finditer(
        r'      name: ([^\n]+)\n[\s\S]*?      internalID: (-?\d+)', meta)}
    assert keys == [entries[i] for i in range(start, start + count)]
    print(f'PASS: {prefab} -> controller -> {clip}, {count} ordered loop frames resolve.')

assert 'transform.localScale =' not in (root / 'Assets/Scripts/Boss/HadesWaveProjectile.cs').read_text()
orb = (root / 'Assets/Prefabs/boss_ball_0.prefab').read_text()
assert 'm_SortingOrder: 2' in orb and 'm_LocalScale: {x: 1, y: 1, z: 1}' in orb
for fid in ['4735846946665035300', '239854699847394319']:
    assert re.search(r'--- !u!212 &' + fid + r'\n[\s\S]*?m_SortingOrder: (\d+)', boss)[1] == ('1' if fid=='4735846946665035300' else '0')
assert 'wakeFrames: []' not in boss
print('PASS: authored projectile size, deterministic head 1/jaw 0 ordering and wake references preserved. Unity not executed.')

stone_block = re.search(r'  stoneFrames:\n((?:  - .*\n)+)', boss)[1]
stone_refs = re.findall(r'fileID: (-?\d+), guid: (\w+)', stone_block)
assert len(stone_refs) == 16
for fid, guid in stone_refs:
    meta = next(p for p in (root / 'Assets/Images/Boss').glob('stone*.png.meta') if 'guid: '+guid in p.read_text())
    assert 'internalID: '+fid in meta.read_text()
assert 'stoneFrameDuration: 1' in boss and 'phaseTwoPower: 1.5' in boss
print('PASS: 16 stone parts form four 1-second stages; phase two damage setting 1.5.')
