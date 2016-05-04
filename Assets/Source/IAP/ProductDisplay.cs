using UnityEngine;
using UnityEngine.UI;

namespace IAP {
    public class ProductDisplay : MonoBehaviour, IPurchaseListener {

        [SerializeField]
        private Button buyButton;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private string productID;

        [SerializeField]
        private string productName;

        private Text buyButtonText;

        private void Start() {
            buyButtonText = buyButton.GetComponentInChildren<Text>();
            Init();
            UpdateStatusText();
        }

        private void Init() {
            buyButtonText.text = "Buy " + GetProductName();

            buyButton.onClick.AddListener(() => {
                IAPManager.PurchaseProduct(productID, this);
            });
        }

        private void UpdateStatusText() {
            statusText.text = ConsumableData.GetQuantity(productID).ToString();
        }

        private string GetProductName() {
            return productName == ""
                   ? productID
                   : productName;
        }

        void IPurchaseListener.OnPurchaseSuccess() {
            UpdateStatusText();
        }

        void IPurchaseListener.OnPurchaseFailure() {}
    }
}