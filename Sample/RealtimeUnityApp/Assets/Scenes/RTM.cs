using System.Collections;
using System.Collections.Generic;
using LeanCloud.Realtime;

public class RTM {
    private static RTM instance = null;

    public LCIMClient Client { get; set; }

    public static RTM Instance {
        get {
            if (instance == null) {
                instance = new RTM();
            }
            return instance;
        }
    }

    private RTM() {}
}
