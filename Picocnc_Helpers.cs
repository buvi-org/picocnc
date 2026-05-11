using System.Numerics;

namespace PicoGK;

public static partial class Picocnc
{
    /// <summary>
    /// Safe logger — uses Log() in viewer mode, Console.WriteLine in headless.
    /// </summary>
    public static void Log(string strFormat, params object[] args)
    {
        try
        {
            Library.Log(strFormat, args);
        }
        catch
        {
            string str = args.Length > 0 ? string.Format(strFormat, args) : strFormat;
            Console.WriteLine(str);
        }
    }

    /// <summary>
    /// Creates a box-shaped Voxels object centered at vecCenter with the given size.
    /// </summary>
    public static Voxels voxBox(Vector3 vecSize, Vector3 vecCenter)
    {
        Mesh msh = Utils.mshCreateCube(vecSize, vecCenter);
        return new Voxels(msh);
    }

    /// <summary>
    /// Creates a cylindrical Voxels object along the specified axis.
    /// </summary>
    public static Voxels voxCylinder(Vector3 vecStart, Vector3 vecEnd, float fDiameter)
    {
        float fRadius = fDiameter / 2f;
        Voxels vox = Voxels.voxLatticeBeam(vecStart, fRadius, vecEnd, fRadius);
        return vox;
    }

    /// <summary>
    /// Creates a cylindrical Voxels object with its main axis along Z, centered at vecCenter.
    /// </summary>
    public static Voxels voxCylinderZ(float fDiameter, float fHeight, Vector3 vecCenter)
    {
        float fHalfH = fHeight / 2f;
        return voxCylinder(
            new Vector3(vecCenter.X, vecCenter.Y, vecCenter.Z - fHalfH),
            new Vector3(vecCenter.X, vecCenter.Y, vecCenter.Z + fHalfH),
            fDiameter);
    }

    /// <summary>
    /// Creates a cylindrical Voxels object with its main axis along Y, centered at vecCenter.
    /// </summary>
    public static Voxels voxCylinderY(float fDiameter, float fLength, Vector3 vecCenter)
    {
        float fHalfL = fLength / 2f;
        return voxCylinder(
            new Vector3(vecCenter.X, vecCenter.Y - fHalfL, vecCenter.Z),
            new Vector3(vecCenter.X, vecCenter.Y + fHalfL, vecCenter.Z),
            fDiameter);
    }

    /// <summary>
    /// Creates a cylindrical Voxels object with its main axis along X, centered at vecCenter.
    /// </summary>
    public static Voxels voxCylinderX(float fDiameter, float fLength, Vector3 vecCenter)
    {
        float fHalfL = fLength / 2f;
        return voxCylinder(
            new Vector3(vecCenter.X - fHalfL, vecCenter.Y, vecCenter.Z),
            new Vector3(vecCenter.X + fHalfL, vecCenter.Y, vecCenter.Z),
            fDiameter);
    }

    /// <summary>
    /// Returns a list of bolt hole positions on a circular pattern.
    /// </summary>
    public static List<Vector3> aBoltCircle(Vector3 vecCenter, float fBoltCircleDia, int nBolts)
    {
        List<Vector3> aPositions = new();
        float fRadius = fBoltCircleDia / 2f;
        for (int i = 0; i < nBolts; i++)
        {
            float fAngle = MathF.PI / 4f + (i * 2f * MathF.PI / nBolts);
            aPositions.Add(new Vector3(
                vecCenter.X + fRadius * MathF.Cos(fAngle),
                vecCenter.Y + fRadius * MathF.Sin(fAngle),
                vecCenter.Z));
        }
        return aPositions;
    }

    /// <summary>
    /// Creates a row of bolt holes (cylindrical subtraction volumes) along a line.
    /// vecHoleAxis is the axis along which the hole is drilled.
    /// </summary>
    public static List<Voxels> aBoltHolesAlongLine(
        Vector3 vecStart, Vector3 vecEnd,
        float fSpacing, float fHoleDia, float fHoleDepth,
        Vector3 vecHoleAxis)
    {
        List<Voxels> aHoles = new();
        float fTotalLen = Vector3.Distance(vecStart, vecEnd);
        Vector3 vecDir = Vector3.Normalize(vecEnd - vecStart);

        for (float d = fSpacing / 2f; d < fTotalLen - fSpacing / 4f; d += fSpacing)
        {
            Vector3 vecPos = vecStart + vecDir * d;
            Vector3 vecTop = vecPos + vecHoleAxis * (fHoleDepth / 2f);
            Vector3 vecBot = vecPos - vecHoleAxis * (fHoleDepth / 2f);
            aHoles.Add(voxCylinder(vecBot, vecTop, fHoleDia));
        }
        return aHoles;
    }

    /// <summary>
    /// Subtracts a list of hole voxels from a target voxel field.
    /// </summary>
    public static void SubtractHoles(ref Voxels voxTarget, List<Voxels> aHoles)
    {
        foreach (Voxels voxHole in aHoles)
            voxTarget.BoolSubtract(voxHole);
    }

    /// <summary>
    /// Exports a Voxels object to an STL file in the user's Documents folder.
    /// </summary>
    public static void ExportStl(Voxels vox, string strName)
    {
        string strPath = Path.Combine(
            Utils.strDocumentsFolder(),
            $"PicoCNC_{strName}.stl");
        vox.mshAsMesh().SaveToStlFile(strPath);
        Log($"Exported: {strPath}");
    }
}
