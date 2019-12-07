namespace csly.whileLang.compiler
{
    public class Signature
    {
        private readonly WhileType left;
        public WhileType Result;
        private readonly WhileType right;

        public Signature(WhileType left, WhileType right, WhileType result)
        {
            this.left = left;
            this.right = right;
            Result = result;
        }

        public bool Match(WhileType l, WhileType r)
        {
            return (left == WhileType.ANY || l == left) &&
                   (right == WhileType.ANY || r == right);
        }
    }
}