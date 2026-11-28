using UnityEngine;
// One number-key dispatch per frame, including UI transitions and mouse selections.
public static class UiNumberShortcuts {
    private static int blockedFrame = -1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() {blockedFrame=-1;}
    public static void BlockThisFrame() {blockedFrame=Time.frameCount;}
    public static bool TryRead(out int number) {
        number=0;
        if(blockedFrame==Time.frameCount)return false;
        for(int i=0;i<3;i++) {
            if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1+i))) {
                number=i+1;BlockThisFrame();return true;
            }
        }
        return false;
    }
}
