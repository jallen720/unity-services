using UnityEngine;

namespace IAP {
    public class NonConsumableHandler : IProductHandler {
        void IProductHandler.OnProductPurchased(string productID) {
            Debug.Log(string.Format("handling nonconsumable: \"{0}\"", productID));
        }
    }
}