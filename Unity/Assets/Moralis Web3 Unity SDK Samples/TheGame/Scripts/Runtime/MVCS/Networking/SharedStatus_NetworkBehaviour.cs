using MoralisUnity.Samples.TheGame.MVCS.Controller.Events;
using MoralisUnity.Samples.TheGame.MVCS.View;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MoralisUnity.Samples.TheGame.MVCS.Networking
{
        //  Namespace Properties ------------------------------


        //  Class Attributes ----------------------------------

        /// <summary>
        /// This is a contrived demo of using RPCs.
        ///
        /// Probably in this specific use case IT WOULD BE BETTER TO USE <see cref="NetworkVariable{T}"/> instead.
        /// However, below is useful to see how RPCs work in this real-world example.
        /// 
        /// </summary>
        public class SharedStatus_NetworkBehaviour : NetworkBehaviour
        {
            //  Events ----------------------------------------
            [HideInInspector]
            public readonly StringUnityEvent OnSharedStatusChanged = new StringUnityEvent();
            public string SharedStatus {  get { return _sharedStatus; } private set { _sharedStatus = value; OnSharedStatusChanged.Invoke(_sharedStatus); }  }
            private string _sharedStatus = "";
            
            //  Properties ------------------------------------
            
            //  Fields ----------------------------------------
                
            //  Unity Methods ---------------------------------

            //  Methods ---------------------------------------
            public void SharedStatusUpdateRequest()
            {
                SharedStatusUpdateRequestServerRpc();
            }


            /// <summary>
            /// **ANY** Client may call the **ONE** server... 
            /// </summary>
            [ServerRpc (RequireOwnership = false)]
            private void SharedStatusUpdateRequestServerRpc(ServerRpcParams serverRpcParams = default)
            {
                string playerName = PlayerView.GetPlayerNameByClientId(serverRpcParams.Receive.SenderClientId);
                string statusText = $"Hi, from {playerName}";
                Debug.Log("RPC: " + statusText);
                SharedStatusUpdateRequestClientRpc(statusText);
            }
 
            
            /// <summary>
            /// ... And the **ONE** server then calls **EVERY** client
            /// </summary>
            [ClientRpc (Delivery = RpcDelivery.Reliable)]
            private void SharedStatusUpdateRequestClientRpc(string sharedStatus)
            {
                SharedStatus = sharedStatus;
            }
            
            
            //  Event Handlers --------------------------------
        }
    }
