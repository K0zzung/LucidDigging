# Source regression guard; not Unity playback or object profiling.
from pathlib import Path
import re, hashlib, math
root=Path(__file__).resolve().parent.parent
def gameplay(text):
 text=text[text.index('    private IEnumerator DashRoutine('):text.index('    private IEnumerator DashMoveToCell(')]
 text=re.sub(r'if \(hitSomething &&\s*CameraShakeController.Instance != null\) \{\s*CameraShakeController.Instance.ShakeDashImpact\(\);\s*\}', '', text)
 text=re.sub(r'if \((?:dealtDamage > 0 && )?dashVFX != null\) dashVFX\.(?:Begin|End|Contact)\([^;]*;', '', text)
 text=re.sub(r'(?:bool )?hitSomething =\s*(?:true|false);', '', text)
 text=re.sub(r'SpawnDashEffect\(direction\);', '', text)
 text=re.sub(r'//[^\n]*', '', text)
 return re.sub(r'\s+', '', text)

source=(root/'Assets/Scripts/Player/PlayerGridController.cs').read_text(errors='surrogateescape')
assert hashlib.sha256(gameplay(source).encode(errors='surrogateescape')).hexdigest()=='b7d3e0578c89008aea68db1142e4fa6fdc56d78368362fda4dbee95f78fe7459'
print('PASS: pre/post Dash gameplay tokens identical after removing visual-only calls. Movement, damage count, waits, invincibility and completion paths preserved.')
# Legacy artwork must be invoked at Begin AND the original per-step point.
routine=source[source.index('    private IEnumerator DashRoutine('):source.index('    private IEnumerator DashMoveToCell(')]
assert routine.count('SpawnDashEffect(direction);') == 2
spawn=source[source.index('    private void SpawnDashEffect('):source.index('    public bool StartDash(')]
assert 'dashVFX.PlayLegacy(dashEffectPrefab, spawnPosition, direction)' in spawn
vfx=(root/'Assets/Scripts/Player/PlayerDashVFX.cs').read_text()
assert 'new DashEffect[5]' in vfx and 'legacy[index].PlayPooled(position, direction)' in vfx
scene=(root/'Assets/Scenes/1655m.unity').read_text()
assert 'guid: 643ebfea221bdb94e85aceced6e09b00' in scene
prefab=(root/'Assets/Prefabs/dash_ef.prefab').read_text()
assert 'guid: 044037f3772df6743a6c48604b01e024' in prefab
print('PASS: original dash_ef prefab/sprite scene reference, Begin + per-step replay path, and bounded 5-slot blue-effect pool retained. Visual appearance requires Unity.')

assert 'LineRenderer' not in vfx and 'Snapshot()' not in vfx
print('PASS: added Dash ghosts/fracture/rays removed; original blue effects only.')
