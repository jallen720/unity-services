using UnityEngine;

namespace IAP {
    public class SubscriptionHandler : IProductHandler {
        void IProductHandler.OnProductPurchased(string productID) {
            Debug.Log(string.Format("handling subscription: \"{0}\"", productID));
        }
    }
}