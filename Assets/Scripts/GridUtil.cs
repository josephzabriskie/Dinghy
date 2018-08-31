using PlayerActions;
using CellInfo;
public static class GridUtils {
    //public static CState[] GridSerialize(CState[,] inGrid, out int dim1, out int dim2){
    public static CState[] Serialize(CState[,] inGrid){
        //dim1 = inGrid.GetLength(0); // prolly don't need these, just check what you send in
        //dim2 = inGrid.GetLength(1);
        CState[] outarray = new CState[inGrid.GetLength(0) * inGrid.GetLength(1)];
        int idx = 0;
        for (int i = 0; i < inGrid.GetLength(0); i ++){
            for (int j = 0; j < inGrid.GetLength(1); j ++){
                outarray[idx++] = inGrid[i,j];
            }
        }
        return outarray;
    }

    public static CState[,]  Deserialize(CState[] inarray, int dim1, int dim2){
        CState[,] outGrid = new CState[dim1, dim2];
        int idx = 0;
        for (int i = 0; i < dim1; i ++){
            for (int j = 0; j < dim2; j ++){
                outGrid[i,j] = inarray[idx++];
            }
        }
        return outGrid;
    }
}