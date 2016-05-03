using UnityEngine;

namespace IAP {
    public class ConsumableHandler : IProductHandler {
        void IProductHandler.OnProductPurchased(string productID) {
            Debug.Log(string.Format("handling consumable: \"{0}\"", productID));
        }
    }
}