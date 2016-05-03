using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityIAPDemo {

    // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity
    // Purchasing.
    public class Purchaser : MonoBehaviour, IStoreListener {
        // Reference to the Purchasing system.
        private static IStoreController storeController;

        // Reference to store-specific Purchasing subsystems.
        private static IExtensionProvider extensionProvider;

        private Dictionary<ProductType, List<GameProduct>> gameProductDatas;

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
            gameProductDatas = GameProductUtil.LoadGameProducts();

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

        private bool IsInitialized() {
            // Only say we are initialized if both the Purchasing references are set.
            return storeController != null && extensionProvider != null;
        }

        public void BuyProduct(string productID) {
            ValidateIsInitialized("BuyProduct");

            // If the stores throw an unexpected exception, use try..catch to protect my logic here.
            try {
                // ... look up the Product reference with the general product identifier and the
                // Purchasing system's products collection.
                Product product = storeController.products.WithID(productID);

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
            catch (Exception e) {
                // ... by reporting any unexpected exception for later diagnosis.
                Debug.Log("BuyProductID: FAIL. Exception during purchase. " + e);
            }
        }

        private void ValidateIsInitialized(string methodName) {
            if (!IsInitialized()) {
                throw new Exception(string.Format(
                    "Purchaser was not initialized prior to calling Purchaser.{0}()",
                    methodName));
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
                Application.platform == RuntimePlatform.OSXPlayer)
            {
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
                    Debug.Log(string.Format(
                        "RestorePurchases continuing: {0}. If no further messages, no purchases " +
                        "available to restore.",
                        result));
                });
            }
            else {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log(string.Format(
                    "RestorePurchases FAIL. Not supported on this platform. Current = {0}",
                    Application.platform));
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
            Debug.Log("OnInitializeFailed InitializationFailureReason: " + error);
        }

        private bool PurchaseIDMatchesProduct(string purchaseID, string productID) {
            return string.Equals(purchaseID, productID, StringComparison.Ordinal);
        }

        private void LogPurchasePass(string purchaseID) {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", purchaseID));
        }

        private void LogUnrecognizedProduct(string purchaseID) {
            Debug.Log(string.Format(
                "ProcessPurchase: FAIL. Unrecognized product: '{0}'",
                purchaseID));
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEventArgs) {
            string purchaseID = purchaseEventArgs.purchasedProduct.definition.id;

            if (ContainsProductForPurchase(purchaseEventArgs)) {
                LogPurchasePass(purchaseID);
            }
            else {
                LogUnrecognizedProduct(purchaseID);
            }

            // Return a flag indicating whether this product has completely been received, or if the
            // application needs to be reminded of this purchase at next app launch. Is useful when
            // saving purchased products to the cloud, and when that save is delayed.
            return PurchaseProcessingResult.Complete;
        }

        private bool ContainsProductForPurchase(PurchaseEventArgs purchaseEventArgs) {
            foreach (List<GameProduct> gameProducts in gameProductDatas.Values) {
                if (gameProducts.Exists(gameProduct =>
                        PurchaseIDMatchesProduct(
                            purchaseEventArgs.purchasedProduct.definition.id,
                            gameProduct.ID)))
                {
                    return true;
                }
            }

            return false;
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