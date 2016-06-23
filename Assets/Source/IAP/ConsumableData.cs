using UnityUtils.DataUtils;

public static class ConsumableData {
    private const int DEFAULT_QUANTITY = 0;

    public static void ChangeQuantity(string productID, int value) {
        SetQuantity(productID, GetQuantity(productID) + value);
    }

    public static int GetQuantity(string productID) {
        return Data.Load(GetKey(productID), DEFAULT_QUANTITY);
    }

    private static string GetKey(string productID) {
        return string.Format("{0}-quantity", productID);
    }

    public static void SetQuantity(string productID, int value) {
        Data.Save(GetKey(productID), value);
    }
}