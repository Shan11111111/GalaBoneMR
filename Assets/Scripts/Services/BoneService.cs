public class BoneService
{
    public GalaBoneInfo GetBone(string meshName)
    {
        switch (meshName)
        {
            case "Scaphoid.L":
                return new GalaBoneInfo
                {
                    meshName = "Scaphoid.L",
                    chineseName = "左側舟狀骨",
                    englishName = "Left Scaphoid",
                    introduction =
                        "舟狀骨（Scaphoid）位於手腕近端腕骨列，是腕骨中最容易發生骨折的骨頭。",
                    structureFunction =
                        "負責連接橈骨與其他腕骨，協助手腕的穩定與活動。",
                    learning =
                        "臨床上舟狀骨骨折十分常見，若沒有及時治療可能造成缺血性壞死。"
                };

            case "Scaphoid.R":
                return new GalaBoneInfo
                {
                    meshName = "Scaphoid.R",
                    chineseName = "右側舟狀骨",
                    englishName = "Right Scaphoid",
                    introduction =
                        "舟狀骨（Scaphoid）位於手腕近端腕骨列，是腕骨中最容易發生骨折的骨頭。",
                    structureFunction =
                        "負責連接橈骨與其他腕骨，協助手腕的穩定與活動。",
                    learning =
                        "臨床上舟狀骨骨折十分常見，若沒有及時治療可能造成缺血性壞死。"
                };

            default:
                return new GalaBoneInfo
                {
                    meshName = meshName,
                    chineseName = "未知骨骼",
                    englishName = meshName,
                    introduction = "目前尚未建立此骨骼介紹。",
                    structureFunction = "尚無資料。",
                    learning = "尚無資料。"
                };
        }
    }
}