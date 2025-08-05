namespace WpfApp1;
using Clipper2Lib;

public class Clean
{
      internal class OutPt
      {
        internal int Idx;
        internal PointD Pt;
        internal OutPt Next;
        internal OutPt Prev;
      };
      
      private static bool PointsAreClose(PointD pt1, PointD pt2, double distSqrd)
      {
        double dx = pt1.x - pt2.x;
        double dy = pt1.y - pt2.y;
        return ((dx * dx) + (dy * dy) <= distSqrd);
      }
      
      private static OutPt ExcludeOp(OutPt op)
      {
        OutPt result = op.Prev;
        result.Next = op.Next;
        op.Next.Prev = result;
        result.Idx = 0;
        return result;
      }
      
      
      private static double DistanceFromLineSqrd(PointD pt, PointD ln1, PointD ln2)
      {
        //The equation of a line in general form (Ax + By + C = 0)
        //given 2 points (x¹,y¹) & (x²,y²) is ...
        //(y¹ - y²)x + (x² - x¹)y + (y² - y¹)x¹ - (x² - x¹)y¹ = 0
        //A = (y¹ - y²); B = (x² - x¹); C = (y² - y¹)x¹ - (x² - x¹)y¹
        //perpendicular distance of point (x³,y³) = (Ax³ + By³ + C)/Sqrt(A² + B²)
        //see http://en.wikipedia.org/wiki/Perpendicular_distance
        double A = ln1.y - ln2.y;
        double B = ln2.x - ln1.x;
        double C = A * ln1.x  + B * ln1.y;
        C = A * pt.x + B * pt.y - C;
        return (C * C) / (A * A + B * B);
      }
      
      private static bool SlopesNearCollinear(PointD pt1, 
        PointD pt2, PointD pt3, double distSqrd)
      {
        //this function is more accurate when the point that's GEOMETRICALLY 
        //between the other 2 points is the one that's tested for distance.  
        //nb: with 'spikes', either pt1 or pt3 is geometrically between the other pts                    
        if (Math.Abs(pt1.x - pt2.x) > Math.Abs(pt1.y - pt2.y))
        {
          if ((pt1.x > pt2.x) == (pt1.x < pt3.x))
            return DistanceFromLineSqrd(pt1, pt2, pt3) < distSqrd;
          else if ((pt2.x > pt1.x) == (pt2.x < pt3.x))
            return DistanceFromLineSqrd(pt2, pt1, pt3) < distSqrd;
          else
            return DistanceFromLineSqrd(pt3, pt1, pt2) < distSqrd;
        }
        else
        {
          if ((pt1.y > pt2.y) == (pt1.y < pt3.y))
            return DistanceFromLineSqrd(pt1, pt2, pt3) < distSqrd;
          else if ((pt2.y > pt1.y) == (pt2.y < pt3.y))
            return DistanceFromLineSqrd(pt2, pt1, pt3) < distSqrd;
          else
            return DistanceFromLineSqrd(pt3, pt1, pt2) < distSqrd;
        }
      }
     public static PathD CleanPolygon(PathD path, double distance = 1.415)
      {
        //distance = proximity in units/pixels below which vertices will be stripped. 
        //Default ~= sqrt(2) so when adjacent vertices or semi-adjacent vertices have 
        //both x & y coords within 1 unit, then the second vertex will be stripped.

        int cnt = path.Count;

        if (cnt == 0) return new PathD();

        OutPt [] outPts = new OutPt[cnt];
        for (int i = 0; i < cnt; ++i) outPts[i] = new OutPt();

        for (int i = 0; i < cnt; ++i)
        {
          outPts[i].Pt = path[i];
          outPts[i].Next = outPts[(i + 1) % cnt];
          outPts[i].Next.Prev = outPts[i];
          outPts[i].Idx = 0;
        }

        double distSqrd = distance * distance;
        OutPt op = outPts[0];
        while (op.Idx == 0 && op.Next != op.Prev)
        {
          if (PointsAreClose(op.Pt, op.Prev.Pt, distSqrd))
          {
            op = ExcludeOp(op);
            cnt--;
          }
          else if (PointsAreClose(op.Prev.Pt, op.Next.Pt, distSqrd))
          {
            ExcludeOp(op.Next);
            op = ExcludeOp(op);
            cnt -= 2;
          }
          else if (SlopesNearCollinear(op.Prev.Pt, op.Pt, op.Next.Pt, distSqrd))
          {
            op = ExcludeOp(op);
            cnt--;
          }
          else
          {
            op.Idx = 1;
            op = op.Next;
          }
        }

        if (cnt < 3) cnt = 0;
        PathD result = new PathD(cnt);
        for (int i = 0; i < cnt; ++i)
        {
          result.Add(op.Pt);
          op = op.Next;
        }
        outPts = null;
        return result;
      }
      //------------------------------------------------------------------------------

      public static PathsD CleanPolygons(PathsD polys,
          double distance = 1.415)
      {
        PathsD result = new PathsD(polys.Count);
        for (int i = 0; i < polys.Count; i++)
          result.Add(CleanPolygon(polys[i], distance));
        return result;
      }
      
}