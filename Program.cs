using PicoGK;

try
{
    Library.Go(
        Picocnc.fVoxelSizeMM,
        Picocnc.Task);
}
catch (Exception e)
{
    Console.WriteLine("Failed to run PicoCNC.");
    Console.WriteLine(e.ToString());
}
