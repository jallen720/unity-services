using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityUtils.Managers;

namespace IAP {
    public partial class IAPManager {
        public static void PurchaseProduct(string productID, IPurchaseListener purchaseListener) {
            try {
                // Look up the Product reference with the general product identifier and the
                // Purchasing system's products collection.
                Product product =
                    Instance
                        .storeController
                        .products
                        .WithID(productID);

                // If the look up found a product for this device's store and that product is ready
                // to be sold.
                if (product != null && product.availableToPurchase) {
                    Debug.Log(string.Format(
                        "Purchasing product asychronously: '{0}'",
                        product.definition.id
                    ));

                    Instance.purchaseRequests.Add(productID, purchaseListener);
                    Instance.storeController.InitiatePurchase(product);
                }
                else {
                    Debug.Log(string.Format(
                        "Can't purchase product '{0}'; either it doesn't match any product IDs " +
                        "or it isn't available for purchase",
                        productID
                    ));
                }
            }
            catch (Exception e) {
                Debug.Log("BuyProductID: FAIL. Exception during purchase: " + e);
            }
        }

        // Restore purchases previously made by this customer. Some platforms automatically restore
        // purchases. Apple currently requires explicit purchase restoration for IAP.
        public static void RestorePurchases() {
            // If we are running on an Apple device ... 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                // ... begin restoring purchases
                Debug.Log("RestorePurchases started ...");

                // Fetch the Apple store-specific subsystem.
                var apple = Instance.extensionProvider.GetExtension<IAppleExtensions>();

                // Begin the asynchronous process of restoring purchases. Expect a confirmation
                // response in the Action<bool> below, and ProcessPurchase if there are previously
                // purchased products to restore.
                apple.RestoreTransactions((bool result) => {
                    // The first phase of restoration. If no more responses are received on
                    // ProcessPurchase then no purchases are available to be restored.
                    Debug.Log(string.Format(
                        "RestorePurchases continuing: {0}. If no further messages, no purchases " +
                        "available to restore.",
                        result
                    ));
                });
            }
            else {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log(string.Format(
                    "RestorePurchases FAIL. Not supported on this platform. Current = {0}",
                    Application.platform
                ));
            }
        }
    }

    public partial class IAPManager : SceneManager<IAPManager>, IStoreListener {
        private readonly Dictionary<ProductType, IProductHandler> productHandlers =
            new Dictionary<ProductType, IProductHandler>() {
                { ProductType.Consumable    , new ConsumableHandler()    },
                { ProductType.NonConsumable , new NonConsumableHandler() },
                { ProductType.Subscription  , new SubscriptionHandler()  },
            };

        private IStoreController storeController;
        private IExtensionProvider extensionProvider;
        private Dictionary<ProductType, List<GameProduct>> gameProductDatas;
        private Dictionary<string, IPurchaseListener> purchaseRequests;

        private void Start() {
            gameProductDatas = GameProductUtil.LoadGameProducts();
            purchaseRequests = new Dictionary<string, IPurchaseListener>();
            Init();
        }

        private void Init() {
            // Create a builder, first passing in a suite of Unity provided stores.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            AddPurchasableProducts(builder);
            UnityPurchasing.Initialize(this, builder);
        }

        private void AddPurchasableProducts(ConfigurationBuilder configurationBuilder) {
            foreach (var gameProductData in gameProductDatas) {
                ProductType productType = gameProductData.Key;
                List<GameProduct> gameProducts = gameProductData.Value;

                foreach (GameProduct gameProduct in gameProducts) {
                    configurationBuilder.AddProduct(gameProduct.ID, productType, new IDs() {
                        { gameProduct.appleID      , AppleAppStore.Name },
                        { gameProduct.googlePlayID , GooglePlay.Name    },
                    });
                }
            }
        }

        void IStoreListener.OnInitialized(
            IStoreController storeController,
            IExtensionProvider extensionProvider)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("OnInitialized: PASS");

            // Overall Purchasing system, configured with products for this application.
            this.storeController = storeController;
            
            // Store specific subsystem, for accessing device-specific store features.
            this.extensionProvider = extensionProvider;
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error) {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this
            // reason with the user.
            Debug.Log("OnInitializeFailed InitializationFailureReason: " + error);
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
            // A product purchase attempt did not succeed. Check failureReason for more detail.
            // Consider sharing this reason with the user.
            Debug.Log(string.Format(
                "OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}",
                product.definition.storeSpecificId,
                failureReason
            ));

            HandlePurchaseRequest(product.definition.id, HandlePurchaseRequestFailure);
        }

        PurchaseProcessingResult
        IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEventArgs) {
            ProductDefinition productDefinition = purchaseEventArgs.purchasedProduct.definition;
            string productID = productDefinition.id;
            ProductType productType = productDefinition.type;

            // Handle purchased product
            ValidateHasHandler(productType);
            productHandlers[productType].OnProductPurchased(productID);

            // Handle product's purchase request
            ValidateIsProduct(productID);
            HandlePurchaseRequest(productID, HandlePurchaseRequestSuccess);

            // Return a flag indicating whether this product has completely been received, or if the
            // application needs to be reminded of this purchase at next app launch. Is useful when
            // saving purchased products to the cloud, and when that save is delayed.
            return PurchaseProcessingResult.Complete;
        }

        private void ValidateIsProduct(string productID) {
            if (!IsProduct(productID)) {
                throw new Exception(string.Format(
                    "product \"{0}\" is not a know product",
                    productID
                ));
            }
        }

        private bool IsProduct(string productID) {
            foreach (List<GameProduct> gameProducts in gameProductDatas.Values) {
                if (MatchesAnyGameProduct(productID, gameProducts)) {
                    return true;
                }
            }

            return false;
        }

        private void ValidateHasHandler(ProductType productType) {
            if (!productHandlers.ContainsKey(productType)) {
                throw new Exception(string.Format(
                    "no product handler for product type: \"{0}\"",
                    productType
                ));
            }
        }

        private bool MatchesAnyGameProduct(string productID, List<GameProduct> gameProducts) {
            return gameProducts.Exists((GameProduct gameProduct) => {
                return string.Equals(
                    productID,
                    gameProduct.ID,
                    StringComparison.Ordinal
                );
            });
        }

        private void ValidateHasPurchaseRequest(string productID) {
            if (!purchaseRequests.ContainsKey(productID)) {
                throw new Exception(string.Format(
                    "no purchase requests for product \"{0}\"",
                    productID
                ));
            }
        }

        private void HandlePurchaseRequest(
            string productID,
            Action<IPurchaseListener> purchaseAction)
        {
            ValidateHasPurchaseRequest(productID);
            purchaseAction(purchaseRequests[productID]);
            purchaseRequests.Remove(productID);
        }

        private void HandlePurchaseRequestSuccess(IPurchaseListener purchaseListener) {
            purchaseListener.OnPurchaseSuccess();
        }

        private void HandlePurchaseRequestFailure(IPurchaseListener purchaseListener) {
            purchaseListener.OnPurchaseFailure();
        }

        private void LogPurchasePass(string purchaseID) {
            Debug.Log(string.Format(
                "ProcessPurchase: PASS. Product: '{0}'",
                purchaseID
            ));
        }

        private void LogUnrecognizedProduct(string purchaseID) {
            Debug.Log(string.Format(
                "ProcessPurchase: FAIL. Unrecognized product: '{0}'",
                purchaseID
            ));
        }
    }
}