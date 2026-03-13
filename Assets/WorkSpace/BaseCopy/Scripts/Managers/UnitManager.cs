using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public Game1Player game1Player;
    public Game1Player Game1Player { get { return game1Player; } set { game1Player = value; } }

    public Game2Player game2Player;
    public Game2Player Game2Player { get { return game2Player; } set { game2Player = value; } }

}
