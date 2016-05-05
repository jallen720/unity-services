using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace IAP {

    [Serializable]
    public struct GameProduct {
        public string ID;
        public string appleID;
        public string googlePlayID;
    }

    public static class GameProductUtil {

        [Serializable]
        private struct ProductDatas {
            public List<ProductData> gameProducts;
        }

        [Serializable]
        private struct ProductData {
            public string type;
            public List<GameProduct> products;

            public ProductType ProductType {
                get {
                    foreach (ProductType productType in Enum.GetValues(typeof(ProductType))) {
                        if (productType.ToString() == type) {
                            return productType;
                        }
                    }

                    throw new Exception(string.Format(
                        "Can't find ProductType value for \"{0}\"",
                        type));
                }
            }
        }

        public static Dictionary<ProductType, List<GameProduct>> LoadGameProducts() {
            string gameProductsJSON = Resources.Load<TextAsset>("JSON/game-products").text;
            var productDatas = JsonUtility.FromJson<ProductDatas>(gameProductsJSON);

            var gameProducts =
                new Dictionary<ProductType, List<GameProduct>>(productDatas.gameProducts.Count);

            foreach (ProductData productData in productDatas.gameProducts) {
                gameProducts.Add(productData.ProductType, productData.products);
            }

            return gameProducts;
        }

        private static void DebugProductDatas(ProductDatas productDatas) {
            var output = "";

            foreach (var productData in productDatas.gameProducts) {
                output += productData.type + " : [\n";

                foreach (var gameProduct in productData.products) {
                    output +=
                        "    gameProduct {\n" +
                        "        ID : " + gameProduct.ID + ",\n" +
                        "        appleID : " + gameProduct.appleID + ",\n" +
                        "        googlePlayID : " + gameProduct.googlePlayID + "\n" +
                        "    }\n";
                }

                output += "]\n\n";
            }

            Debug.Log(output);
        }
    }
}