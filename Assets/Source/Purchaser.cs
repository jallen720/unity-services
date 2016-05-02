using System;
using UnityEngine;
using UnityEngine.Purchasing;

// Placing the Purchaser class in the CompleteProject namespace allows it to interact with
// ScoreManager, one of the existing Survival Shooter scripts.
namespace CompleteProject {

    // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity
    // Purchasing.
    public class Purchaser : MonoBehaviour, IStoreListener {
        // Reference to the Purchasing system.
        private static IStoreController storeController;

        // Reference to store-specific Purchasing subsystems.
        private static IExtensionProvider extensionProvider;

        // Product identifiers for all products capable of being purchased: "convenience" general
        // identifiers for use with Purchasing, and their store-specific identifier counterparts for
        // use with and outside of Unity Purchasing. Define store-specific identifiers also on each
        // platform's publisher dashboard (iTunes Connect, Google Play Developer Console, etc.)
        private const string PRODUCT_ID_CONSUMABLE = "consumable";
        private const string PRODUCT_ID_NONCONSUMABLE = "nonconsumable";
        private const string PRODUCT_ID_SUBSCRIPTION = "subscription";


        private struct GameProduct {
            public string ID;
            public string appleID;
            public string googlePlayID;
            public ProductType type;
        }


        private readonly GameProduct[] gameProducts = new GameProduct[] {
            new GameProduct() {
                ID           = PRODUCT_ID_CONSUMABLE,
                appleID      = "com.unity3d.test.services.purchasing.consumable",
                googlePlayID = "com.unity3d.test.services.purchasing.consumable",
                type         = ProductType.Consumable,
            },

            new GameProduct() {
                ID           = PRODUCT_ID_NONCONSUMABLE,
                appleID      = "com.unity3d.test.services.purchasing.nonconsumable",
                googlePlayID = "com.unity3d.test.services.purchasing.nonconsumable",
                type         = ProductType.NonConsumable,
            },

            new GameProduct() {
                ID           = PRODUCT_ID_SUBSCRIPTION,
                appleID      = "com.unity3d.test.services.purchasing.subscription",
                googlePlayID = "com.unity3d.test.services.purchasing.subscription",
                type         = ProductType.Subscription,
            },
        };


        private void Start() {
            // If we haven't set up the Unity Purchasing reference
            if (storeController == null) {
                // Begin to configure our connection to Purchasing
                InitializePurchasing();
            }
        }


        public void InitializePurchasing() {
            // If we have already connected to Purchasing ...
            if (IsInitialized()) {
                // ... we are done here.
                return;
            }

            // Create a builder, first passing in a suite of Unity provided stores.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            AddPurchasableProducts(builder);
            UnityPurchasing.Initialize(this, builder);
        }


        private void AddPurchasableProducts(ConfigurationBuilder configurationBuilder) {
            foreach (GameProduct gameProduct in gameProducts) {
                configurationBuilder.AddProduct(gameProduct.ID, gameProduct.type, new IDs() {
                    { gameProduct.appleID      , AppleAppStore.Name },
                    { gameProduct.googlePlayID , GooglePlay.Name    },
                });
            }
        }


        private bool IsInitialized() {
            // Only say we are initialized if both the Purchasing references are set.
            return storeController != null && extensionProvider != null;
        }


        public void BuyConsumable() {
            // Buy the consumable product using its general identifier. Expect a response either
            // through ProcessPurchase or OnPurchaseFailed asynchronously.
            BuyProductID(PRODUCT_ID_CONSUMABLE);
        }


        public void BuyNonConsumable() {
            // Buy the non-consumable product using its general identifier. Expect a response either
            // through ProcessPurchase or OnPurchaseFailed asynchronously.
            BuyProductID(PRODUCT_ID_NONCONSUMABLE);
        }


        public void BuySubscription() {
            // Buy the subscription product using its the general identifier. Expect a response
            // either through ProcessPurchase or OnPurchaseFailed asynchronously.
            BuyProductID(PRODUCT_ID_SUBSCRIPTION);
        }


        private void BuyProductID(string productId) {
            // If the stores throw an unexpected exception, use try..catch to protect my logic here.
            try {
                // If Purchasing has been initialized ...
                if (IsInitialized()) {
                    // ... look up the Product reference with the general product identifier and the
                    // Purchasing system's products collection.
                    Product product = storeController.products.WithID(productId);

                    // If the look up found a product for this device's store and that product is
                    // ready to be sold ... 
                    if (product != null && product.availableToPurchase) {

                        // ... buy the product. Expect a response either through ProcessPurchase or
                        // OnPurchaseFailed asynchronously.
                        Debug.Log(
                            string.Format("Purchasing product asychronously: '{0}'",
                            product.definition.id));

                        storeController.InitiatePurchase(product);
                    }
                    else {
                        // ... report the product look-up failure situation  
                        Debug.Log(
                            "BuyProductID: FAIL. Not purchasing product, either is not found or " +
                            "is not available for purchase");
                    }
                }
                else {
                    // ... report the fact Purchasing has not succeeded initializing yet. Consider
                    // waiting longer or retrying initiailization.
                    Debug.Log("BuyProductID FAIL. Not initialized.");
                }
            }
            // Complete the unexpected exception handling ...
            catch (Exception e) {
                // ... by reporting any unexpected exception for later diagnosis.
                Debug.Log("BuyProductID: FAIL. Exception during purchase. " + e);
            }
        }


        // Restore purchases previously made by this customer. Some platforms automatically restore
        // purchases. Apple currently requires explicit purchase restoration for IAP.
        public void RestorePurchases() {
            // If Purchasing has not yet been set up ...
            if (!IsInitialized()) {
                // ... report the situation and stop restoring. Consider either waiting longer, or
                // retrying initialization.
                Debug.Log("RestorePurchases FAIL. Not initialized.");
                return;
            }

            // If we are running on an Apple device ... 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer) {
                // ... begin restoring purchases
                Debug.Log("RestorePurchases started ...");

                // Fetch the Apple store-specific subsystem.
                var apple = extensionProvider.GetExtension<IAppleExtensions>();

                // Begin the asynchronous process of restoring purchases. Expect a confirmation
                // response in the Action<bool> below, and ProcessPurchase if there are previously
                // purchased products to restore.
                apple.RestoreTransactions((bool result) => {
                    // The first phase of restoration. If no more responses are received on
                    // ProcessPurchase then no purchases are available to be restored.
                    Debug.Log(
                        "RestorePurchases continuing: "
                        + result
                        + ". If no further messages, no purchases available to restore.");
                });
            }
            else {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log(
                    "RestorePurchases FAIL. Not supported on this platform. Current = "
                    + Application.platform);
            }
        }


        //  
        // --- IStoreListener
        //

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("OnInitialized: PASS");

            // Overall Purchasing system, configured with products for this application.
            storeController = controller;
            
            // Store specific subsystem, for accessing device-specific store features.
            extensionProvider = extensions;
        }


        public void OnInitializeFailed(InitializationFailureReason error) {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this
            // reason with the user.
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }


        private bool PurchaseIDMatches(string purchaseID, string productID) {
            return String.Equals(purchaseID, productID, StringComparison.Ordinal);
        }


        private void LogPurchasePass(string purchaseID) {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", purchaseID));
        }


        private void LogUnrecognizedProduct(string purchaseID) {
            Debug.Log(string.Format(
                "ProcessPurchase: FAIL. Unrecognized product: '{0}'",
                purchaseID));
        }


        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
            string purchaseID = args.purchasedProduct.definition.id;

            // A consumable product has been purchased by this user.
            if (PurchaseIDMatches(purchaseID, PRODUCT_ID_CONSUMABLE)) {
                // If the consumable item has been successfully purchased, add 100 coins to the
                // player's in-game score.
                LogPurchasePass(purchaseID);
            }
            // A non-consumable product has been purchased by this user.
            else if (PurchaseIDMatches(purchaseID, PRODUCT_ID_NONCONSUMABLE)) {
                LogPurchasePass(purchaseID);
            }
            // A subscription product has been purchased by this user.
            else if (PurchaseIDMatches(purchaseID, PRODUCT_ID_SUBSCRIPTION)) {
                LogPurchasePass(purchaseID);
            }
            // An unknown product has been purchased by this user. Fill in additional products here.
            else {
                LogUnrecognizedProduct(purchaseID);
            }

            // Return a flag indicating wither this product has completely been received, or if the
            // application needs to be reminded of this purchase at next app launch. Is useful when
            // saving purchased products to the cloud, and when that save is delayed.
            return PurchaseProcessingResult.Complete;
        }


        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
            // A product purchase attempt did not succeed. Check failureReason for more detail.
            // Consider sharing this reason with the user.
            Debug.Log(string.Format(
                "OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}",
                product.definition.storeSpecificId,
                failureReason));
        }
    }
}