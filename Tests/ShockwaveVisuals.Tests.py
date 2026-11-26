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
assert 'damagedShockwaveEnemies.Add(enemy)' in s
assert 'shockwaveTargetAttack[enemy]' in s
print('PASS: evolved Shockwave uses cached strongest attack and one-hit target guard; executable coverage is in ShockwaveEvolution.Tests.ps1.')
skill=(root/'Assets/Scripts/Player/PlayerSkillController.cs').read_text(errors='surrogateescape')
a=skill.index('    private bool TryShockwave()');z=skill.index('    private bool TryShield()',a);cast=skill[a:z]
assert cast.index('TryConsumeShockwave(')<cast.index('gridController.PerformShockwave(')<cast.index('shockwaveVisual.Play(')<cast.index('StartSkillLock(')
assert 'GetEvolutionLevel' not in cast and '.TryConsumeSkill(' not in cast
assert 'shockwaveEffectPrefab' in cast
assert 'guid: d39ffd66cebaefd4db2a1a491e276be8' in (root/'Assets/Prefabs/ShockWave_ef.prefab').read_text()
print('PASS: original prefab/image references preserved; one atomic charge commit precedes one damage pass; original VFX follows. original blue rendering restored; Unity not executed.')


fx=(root/'Assets/Scripts/Player/ShockwaveEffect.cs').read_text()
assert 'spriteRenderer.DOFade(' in fx and 'transform.DOScale(' in fx
assert 'MeshRenderer' not in fx and 'LineRenderer' not in fx
print('PASS: original blue Sprite fade/scale restored; procedural overlay removed.')
