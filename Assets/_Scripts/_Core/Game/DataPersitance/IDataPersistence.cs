public interface IDataPersistence 
{
    void LoadData(GameData data);
    void SaveData(ref GameData data);

    void LoadData(HangarData data);
    void SaveData(ref HangarData data);
}
