public interface ISaveable
{
    // Called when saving: Script puts its data into 'data'
    void SaveData(SaveData data);

    // Called when loading: Script reads its data from 'data'
    void LoadData(SaveData data);
}