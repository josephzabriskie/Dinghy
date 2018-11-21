using PlayerActions;
using CellTypes;
public static class GUtils {
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

    public static CState[,] ApplyHiddenMask(CState[,] inGrid, bool[,] hiddenMask){
        CState[,] outGrid = new CState[inGrid.GetLength(0), inGrid.GetLength(1)];
        for (int i = 0; i < inGrid.GetLength(0); i ++){
            for (int j = 0; j < inGrid.GetLength(1); j ++){
                if(hiddenMask[i,j]){ // Vis mask. 1 means we can't see it, 1 means we can
                    outGrid[i,j] = CState.hidden;
                }
                else{
                    outGrid[i,j] = inGrid[i,j];
                }
            }
        }
        return outGrid;
    }


    	// helper function that I'd like to make local, but this version of c# doesn't support that :(
	public static void FillGrid(CState[,] inGrid, CState s){
		for (int i = 0; i < inGrid.GetLength(0); i++){
			for (int j = 0; j < inGrid.GetLength(1); j++){
				inGrid[i,j] = s;
			}
		}
	}

    public static void FillBoolGrid(bool[,] inGrid, bool b){
        for (int i = 0; i < inGrid.GetLength(0); i++){
			for (int j = 0; j < inGrid.GetLength(1); j++){
				inGrid[i,j] = b;
			}
        }
    }
}