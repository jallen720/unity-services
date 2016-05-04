namespace IAP {
    public class ConsumableHandler : IProductHandler {
        void IProductHandler.OnProductPurchased(string productID) {
            ConsumableData.ChangeQuantity(productID, 1);
        }
    }
}