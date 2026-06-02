using System.Collections.Generic;

namespace Bike.Helpers
{
    public static class TranslationHelper
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["en"] = new()
            {
                ["Dashboard"] = "Dashboard",
                ["Add"] = "Add",
                ["History"] = "History",
                ["Monthly"] = "Monthly",
                ["AvgEfficiency"] = "AVG EFFICIENCY",
                ["TotalFuel"] = "TOTAL FUEL",
                ["ThisMonthCost"] = "THIS MONTH COST",
                ["TotalDistance"] = "TOTAL DISTANCE",
                ["LatestEfficiency"] = "LATEST EFFICIENCY",
                ["AddFuelLog"] = "Add Fuel Log",
                ["Date"] = "Date",
                ["FuelLiters"] = "Fuel (L)",
                ["DistanceKm"] = "Distance (km)",
                ["Cost"] = "Cost (optional)",
                ["Save"] = "Save",
                ["FuelHistory"] = "Fuel History",
                ["Records"] = "records",
                ["Record"] = "record",
                ["MonthlyCost"] = "Monthly Cost",
                ["OneMonth"] = "1 month",
                ["Fuel"] = "fuel",
                ["Distance"] = "distance",
                ["Efficiency"] = "efficiency",
                ["DeleteConfirm"] = "Are you sure you want to delete?"
            },
            ["ja"] = new()
            {
                ["Dashboard"] = "ダッシュボード",
                ["Add"] = "追加",
                ["History"] = "履歴",
                ["Monthly"] = "月次",
                ["AvgEfficiency"] = "平均燃費",
                ["TotalFuel"] = "合計燃料",
                ["ThisMonthCost"] = "今月の費用",
                ["TotalDistance"] = "合計距離",
                ["LatestEfficiency"] = "直近の燃費",
                ["AddFuelLog"] = "給油記録を追加",
                ["Date"] = "日付",
                ["FuelLiters"] = "燃料 (L)",
                ["DistanceKm"] = "距離 (km)",
                ["Cost"] = "費用 (任意)",
                ["Save"] = "保存",
                ["FuelHistory"] = "給油履歴",
                ["Records"] = "件",
                ["Record"] = "件",
                ["MonthlyCost"] = "月間費用",
                ["OneMonth"] = "1ヶ月",
                ["Fuel"] = "燃料",
                ["Distance"] = "距離",
                ["Efficiency"] = "燃費",
                ["DeleteConfirm"] = "本当に削除しますか？"
            },
            ["vi"] = new()
            {
                ["Dashboard"] = "Bảng điều khiển",
                ["Add"] = "Thêm mới",
                ["History"] = "Lịch sử",
                ["Monthly"] = "Hàng tháng",
                ["AvgEfficiency"] = "HIỆU SUẤT TB",
                ["TotalFuel"] = "TỔNG NHIÊN LIỆU",
                ["ThisMonthCost"] = "CHI PHÍ THÁNG NÀY",
                ["TotalDistance"] = "TỔNG QUÃNG ĐƯỜNG",
                ["LatestEfficiency"] = "HIỆU SUẤT MỚI NHẤT",
                ["AddFuelLog"] = "Thêm nhật ký nhiên liệu",
                ["Date"] = "Ngày",
                ["FuelLiters"] = "Nhiên liệu (L)",
                ["DistanceKm"] = "Khoảng cách (km)",
                ["Cost"] = "Chi phí (tùy chọn)",
                ["Save"] = "Lưu",
                ["FuelHistory"] = "Lịch sử nhiên liệu",
                ["Records"] = "bản ghi",
                ["Record"] = "bản ghi",
                ["MonthlyCost"] = "Chi phí hàng tháng",
                ["OneMonth"] = "1 tháng",
                ["Fuel"] = "nhiên liệu",
                ["Distance"] = "khoảng cách",
                ["Efficiency"] = "hiệu suất",
                ["DeleteConfirm"] = "Bạn có thực sự muốn xóa không?"
            }
        };

        public static string T(string key, string lang)
        {
            lang = lang?.ToLower() ?? "en";
            if (!Translations.ContainsKey(lang)) lang = "en";
            
            if (Translations[lang].TryGetValue(key, out var translation))
            {
                return translation;
            }
            
            return Translations["en"].TryGetValue(key, out var enTranslation) ? enTranslation : key;
        }
    }
}