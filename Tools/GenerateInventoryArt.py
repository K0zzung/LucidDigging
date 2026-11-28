"""Deterministic local pixel art; previews are design verification, not Unity renders."""
from pathlib import Path
import json
import uuid
import re
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Assets/Images/UI/RunInventory'
PREVIEW = ROOT / 'Artifacts/Inventory/Previews'
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
INK = '#121125'
EDGE = '#332754'
PURPLE = '#8762bf'
LIGHT = '#c8a9ec'
CYAN = '#67eee2'


def bevel(draw, box, color, cut=2):
    x, y, r, b = box
    draw.polygon([(x+cut,y),(r-cut,y),(r,y+cut),(r,b-cut),
                  (r-cut,b),(x+cut,b),(x,b-cut),(x,y+cut)], fill=color)


panel = Image.new('RGBA', (144, 152))
d = ImageDraw.Draw(panel)
bevel(d, (1,1,142,150), '#100e22', 5)
bevel(d, (2,2,141,149), PURPLE, 4)
bevel(d, (4,4,139,147), EDGE, 3)
bevel(d, (6,6,137,145), '#19152d', 2)
d.line((7,6,136,6), fill=LIGHT)
d.line((6,7,6,144), fill='#68508e')
d.line((7,145,136,145), fill='#241b40')
for x in (9,134):
    for y in (10,141):
        d.rectangle((x-1,y-1,x+1,y+1), fill='#a780d2')
        d.point((x,y), fill='#eee0fb')
for y in (22,76):
    d.line((13,y,130,y), fill='#37294f')
    d.line((60,y,83,y), fill='#6c4d92')
    d.polygon([(71,y-2),(73,y),(71,y+2),(69,y)], fill=PURPLE)

slot = Image.new('RGBA', (32,32))
d = ImageDraw.Draw(slot)
bevel(d,(1,1,30,30),'#100e20',3)
bevel(d,(2,2,29,29),'#6e519a',2)
bevel(d,(4,4,27,27),'#302642',1)
bevel(d,(5,5,26,26),'#100f21',1)
d.line((6,5,25,5), fill='#44345d')
d.line((5,6,5,25), fill='#352a4a')

outline = Image.new('RGBA', (32,32))
d = ImageDraw.Draw(outline)
bevel(d,(1,1,30,30),CYAN,3)
bevel(d,(3,3,28,28),'#2ca4ad',2)
bevel(d,(4,4,27,27),(0,0,0,0),1)
# All corner accents mirror about both canvas axes.
for x in (3,28):
    for y in (3,28):
        d.point((x,y),fill='#d4fffa')

handle = Image.new('RGBA',(16,40))
d = ImageDraw.Draw(handle)
bevel(d,(1,1,14,38),'#211a38',3)
bevel(d,(2,2,13,37),'#9470c5',2)
bevel(d,(4,4,11,35),'#28203f',1)
d.line((5,6,10,6),fill='#cfafea')
d.line((5,33,10,33),fill='#514067')

assets = {'InventoryPanel':panel,'InventorySlot':slot,
          'EquippedOutline':outline,'InventoryHandle':handle}


def meta(path):
    target = Path(str(path)+'.meta')
    if target.exists():
        return
    target.write_text(f'''fileFormatVersion: 2
guid: {uuid.uuid4().hex}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spritePixelsToUnits: 100
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    nameFileIdTable: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
''')


def checker(size):
    im = Image.new('RGBA',size,'#282633')
    draw = ImageDraw.Draw(im)
    for y in range(0,size[1],8):
        for x in range(0,size[0],8):
            if (x//8+y//8)%2:
                draw.rectangle((x,y,x+7,y+7),fill='#3d394b')
    return im


report = {}
for name, im in assets.items():
    path = OUT / (name+'.png')
    im.save(path)
    meta(path)
    Image.open(path).verify()
    alpha = im.getchannel('A')
    assert set(alpha.getdata()) <= {0,255}
    report[name] = {'size':im.size,'alpha_values':sorted(set(alpha.getdata())),
                    'opaque_bounds':alpha.getbbox()}
    large = im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST)
    preview = checker(large.size)
    preview.alpha_composite(large)
    preview.convert('RGB').save(PREVIEW/(name+'.preview.png'))

assert outline.size == slot.size
assert outline.getchannel('A').getbbox() == (1,1,31,31)
assert outline.getchannel('A').crop((5,5,27,27)).getbbox() is None
assert outline.tobytes() == ImageOps.mirror(outline).tobytes()
assert outline.tobytes() == ImageOps.flip(outline).tobytes()
overlap = Image.alpha_composite(slot,outline).resize((256,256),Image.Resampling.NEAREST)
checker(overlap.size).convert('RGBA')  # Never change original alpha for delivery.
overlap.save(PREVIEW/'SlotOutlineOverlay.preview.png')

# Standalone layout proof. Solid background, no cropped gameplay/HUD/reference image.
canvas = Image.new('RGBA',(1080,1920),'#252238')
draw = ImageDraw.Draw(canvas)
font_path = ROOT/'Assets/TextMesh Pro/Fonts/neodgm.ttf'
font = ImageFont.truetype(str(font_path),24)
small = ImageFont.truetype(str(font_path),18)
draw.text((40,60),'LAYOUT PROOF / NOT A UNITY RENDER',font=font,fill='#b8a5ce')
draw.text((40,180),'HP / Q  W  E : existing HUD unchanged',font=small,fill='#a995bd')
x,y=12,300
canvas.alpha_composite(panel.resize((432,456),Image.Resampling.NEAREST),(x,y))
canvas.alpha_composite(handle.resize((48,120),Image.Resampling.NEAREST),(x+432,y+168))
draw.text((x+216,y+18),'장비',font=font,fill='#eadcf5',anchor='mt')
draw.text((x+24,y+70),'무기',font=font,fill='#cab0e7')
draw.text((x+24,y+244),'곡괭이',font=font,fill='#cab0e7')
def equipment_preview(asset):
    text = (ROOT/'Assets/Datas'/asset).read_text(encoding='utf-8-sig')
    match = re.search(r'inventoryIcon: \{fileID: (-?\d+), guid: (\w+)', text)
    source = ROOT / ('Assets/Images/Weapons/sword.png' if match[2]=='2b31f39c25f947b408245a4951e9f249' else 'Assets/Datas/arondight.png')
    icon = Image.open(source).convert('RGBA')
    meta_text = Path(str(source)+'.meta').read_text(encoding='utf-8-sig')
    if match[1] != '21300000':
        for sprite in re.finditer(r'      name: [^\n]+\n      rect:([\s\S]*?)      internalID: (-?\d+)', meta_text):
            if sprite[2] == match[1]:
                sx,sy,sw,sh = map(int,re.search(r'x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)',sprite[1]).groups())
                icon = icon.crop((sx,icon.height-sy-sh,sx+sw,icon.height-sy))
                break
        else:
            raise AssertionError('Inventory icon Sprite ID is missing')
    factor=min(72/icon.width,72/icon.height)
    icon=icon.resize((round(icon.width*factor),round(icon.height*factor)),Image.Resampling.NEAREST)
    return icon,re.search(r'weaponName: ([^\n]+)',text)[1].strip()

examples = [equipment_preview(name) for name in ('StoneSword.asset','HadeSword.asset','DragonSword.asset')]
for row, top in enumerate((102,280)):
    for index in range(3):
        xx=x+48+120*index
        canvas.alpha_composite(slot.resize((96,96),Image.Resampling.NEAREST),(xx,y+top))
        if row==0 and index==0:
            canvas.alpha_composite(outline.resize((96,96),Image.Resampling.NEAREST),(xx,y+top))
        if row==0:
            icon,name = examples[index]
            canvas.alpha_composite(icon,(xx+(96-icon.width)//2,y+top+(96-icon.height)//2))
        else:
            name='미연결'
        draw.text((xx+2,y+top+102),name,font=small,fill='#9b8aac')
draw.text((x+216,y+416),'곡괭이 데이터 미연결',font=small,fill='#bba8d0',anchor='mt')
draw.text((x+456,y+228),'<',font=font,fill='#eadcf5',anchor='mm')
canvas.convert('RGB').save(PREVIEW/'InventoryComposite.preview.png')
(PREVIEW.parent/'AssetValidation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
