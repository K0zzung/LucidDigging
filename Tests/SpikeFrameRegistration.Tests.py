"""Asset-coordinate and reference-animation checks. Does not render Unity."""
from pathlib import Path
import re
from PIL import Image, ImageChops

root = Path(__file__).resolve().parent.parent
prefab = (root / 'Assets/Prefabs/spike_9.prefab').read_text()

def sprite_table(name):
    text = (root / ('Assets/Images/Boss/' + name + '.png.meta')).read_text()
    ppu = float(re.search(r'spritePixelsToUnits: ([\d.]+)', text)[1])
    result = {}
    for match in re.finditer(r'      name: ([^\n]+)\n      rect:([\s\S]*?)      internalID: (-?\d+)', text):
        rect = tuple(map(int, re.search(r'        x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)', match[2]).groups()))
        pivot = tuple(map(float, re.search(r"pivot: \{x: ([\d.]+), y: ([\d.]+)\}", match[2]).groups()))
        result[match[3]] = (rect, pivot, ppu)
    return result

for prefix, name, count in [('rise', 'spike', 7), ('down', 'spikeDown', 10)]:
    sprites = sprite_table(name)
    frames = re.search(r'  '+prefix+r'Frames:\n([\s\S]*?)  '+prefix+r'Offsets:', prefab)[1]
    ids = re.findall(r'fileID: (-?\d+)', frames)
    values = re.search(r'  '+prefix+r'Offsets:\n([\s\S]*?)  '+prefix+r'Keys:', prefab)[1]
    offsets = [tuple(map(float, v)) for v in re.findall(r'x: ([-.\d]+), y: ([-.\d]+)', values)]
    assert len(ids) == len(offsets) == count
    for sprite_id, (ox, oy) in zip(ids, offsets):
        (x, y, width, height), (px, py), ppu = sprites[sprite_id]
        assert abs(ox - (x % 132 + width*px - 66)/ppu) < 1e-7
        assert abs(oy - (y + height*py - 34)/ppu) < 1e-7
    print(f'PASS: {count} {prefix} frames share sheet-space X=66 / ground Y=34; crop offsets cancel displacement.')

up = Image.open(root / 'Assets/Images/Boss/spike.png')
down = Image.open(root / 'Assets/Images/Boss/spikeDown.png')
assert ImageChops.difference(up.crop((828,19,891,155)), down.crop((36,19,99,155))).getbbox() is None
print('PASS: last rise / first down frame pixels match exactly; current Sprite IDs, PPU and Pivot are used without changing Import settings.')
source = (root/'Assets/Scripts/Boss/BossSpikeStrike.cs').read_text()
assert 'transform.localScale = Vector3.one*scale' in source
assert '3f*cellHeight*riseFrames[riseFrames.Length-1].pixelsPerUnit/140f' in source
assert 'groundPosition + transform.localRotation*new Vector3(offset.x*transform.localScale.x,offset.y*transform.localScale.y,0f)' in source
assert 'transform.localPosition=groundPosition' in source
assert 'if(!hit && age>=HitDelay)' in source
print('PASS: uniform 3-cell display size; registered baseline, single damage gate and final anchor restore present. Unity visual testing pending.')
