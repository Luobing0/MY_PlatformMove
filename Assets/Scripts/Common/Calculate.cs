public static class Calculate{
    public static bool OnInterval(float val, float prevVal, float interval){
        return (int)(prevVal / interval) != (int)(val / interval);
    }
}