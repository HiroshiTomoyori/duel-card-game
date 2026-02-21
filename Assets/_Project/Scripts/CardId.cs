public enum Suit { Spade, Heart, Diamond, Club }

public struct CardId
{
    public Suit suit;
    public int rank; // 1..13 (A..K)

    public CardId(Suit suit, int rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

    public override string ToString() => $"{suit}-{rank}";
}
