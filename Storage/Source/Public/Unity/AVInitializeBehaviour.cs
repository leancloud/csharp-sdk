using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LeanCloud
{
    public class AVCoreExtensions : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void OnRuntimeMethodLoad() {
            var go = new GameObject {
                name = "AVCoreExtensions"
            };
            DontDestroyOnLoad(go);
            var avce = go.AddComponent<AVCoreExtensions>();

            Dispatcher.Instance.GameObject = go;

            // Kick off the dispatcher.
            avce.StartCoroutine(Dispatcher.Instance.DispatcherCoroutine);

            AVInitializeBehaviour.IsWebPlayer = Application.platform == RuntimePlatform.WebGLPlayer;
        }
    }

    /// <summary>
    /// Mandatory MonoBehaviour for scenes that use LeanCloud. Set the application ID and .NET key
    /// in the editor.
    /// </summary>
    // TODO (hallucinogen): somehow because of Push, we need this class to be added in a GameObject
    // called `AVInitializeBehaviour`. We might want to fix this.
    public class AVInitializeBehaviour : MonoBehaviour
    {
        [SerializeField]
        /// <summary>
        /// The LeanCloud applicationId used in this app. You can get this value from the LeanCloud website.
        /// </summary>
        public string applicationID;


        [SerializeField]
        /// <summary>
        /// The LeanCloud applicationKey used in this app. You can get this value from the LeanCloud website.
        /// </summary>
        public string applicationKey;

        [SerializeField]
        /// <summary>
        /// The service region.
        /// </summary>
        public AVClient.Configuration.AVRegion region;


        [SerializeField]
        /// <summary>
        /// Use this uri as cloud function server host. This is used for local development.
        /// </summary>
        public string engineServer;

        [SerializeField]
        /// <summary>
        /// Whether use production stage to process request or not.
        /// </summary>
        public bool useProduction = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:LeanCloud.AVInitializeBehaviour"/> is web player.
        /// </summary>
        /// <value><c>true</c> if is web player; otherwise, <c>false</c>.</value>
        public static bool IsWebPlayer { get; set; }
    }
}
