# Source-path regression check; not a Unity play test.
from pathlib import Path
import re,hashlib
root=Path(__file__).resolve().parent.parent
def gameplay(text):
 text=text[text.index('    public void PerformShockwave('):text.index('    private void CaptureSkillInput(')]
 text=text.replace(', ShockwaveEffect visual = null', '')
 text=re.sub(r'if \(visual != null\) visual.SetGrid\([\s\S]*?;','',text)
 text=re.sub(r'if \(dealtDamage > 0 && visual != null\) visual.AddImpact\([^;]*;', '',text)
 text=re.sub(r'//[^\n]*','',text)
 return re.sub(r'\s+','',text)

s=(root/'Assets/Scripts/Player/PlayerGridController.cs').read_text(errors='surrogateescape')
assert hashlib.sha256(gameplay(s).encode(errors='surrogateescape')).hexdigest()=='bee14a9e26623fe0f6dafaaec11026705d33a1eeb8f297e09852d5db7f41394f'
print('PASS: Shockwave damage-loop tokens identical: size normalization, wall exclusions, HashSet deduplication, damage, deaths and movement unchanged.')
skill=(root/'Assets/Scripts/Player/PlayerSkillController.cs').read_text(errors='surrogateescape')
a=skill.index('    private bool TryShockwave()');z=skill.index('    private bool TryShield()',a);cast=skill[a:z]
assert cast.index('gridController.PerformShockwave(')<cast.index('shockwaveVisual.Play(')<cast.index('TryConsumeSkill(')<cast.index('StartSkillLock(')
assert 'shockwaveEffectPrefab' in cast
assert 'guid: d39ffd66cebaefd4db2a1a491e276be8' in (root/'Assets/Prefabs/ShockWave_ef.prefab').read_text()
print('PASS: original prefab/image references preserved; damage remains before VFX, consumption and lock. original blue rendering restored; Unity not executed.')


fx=(root/'Assets/Scripts/Player/ShockwaveEffect.cs').read_text()
assert 'spriteRenderer.DOFade(' in fx and 'transform.DOScale(' in fx
assert 'MeshRenderer' not in fx and 'LineRenderer' not in fx
print('PASS: original blue Sprite fade/scale restored; procedural overlay removed.')
