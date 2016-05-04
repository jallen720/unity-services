using UnityUtils.DataUtils;

public static class ConsumableData {
    public static void ChangeQuantity(string productID, int value) {
        SetQuantity(productID, GetQuantity(productID) + 1);
    }

    public static int GetQuantity(string productID) {
        return Data.Load(GetKey(productID), 0);
    }

    private static string GetKey(string productID) {
        return string.Format("{0}-quantity", productID);
    }

    public static void SetQuantity(string productID, int value) {
        Data.Save(GetKey(productID), value);
    }
}