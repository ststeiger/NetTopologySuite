using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NUnit.Framework;

namespace NetTopologySuite.Tests.NUnit.Geometries
{
    [TestFixture]
    public class GeometryImplTest
    {
        private PrecisionModel precisionModel;
        private GeometryFactory geometryFactory;
        WKTReader reader;
        WKTReader readerFloat;

        public GeometryImplTest()
        {
            precisionModel = new PrecisionModel(1);
            geometryFactory = new GeometryFactory(precisionModel, 0);
            reader = new WKTReader(geometryFactory);
            readerFloat = new WKTReader();
        }

        [Test]
        public void TestComparable()
        {
            var point = reader.Read("POINT EMPTY");
            var lineString = reader.Read("LINESTRING EMPTY");
            var linearRing = reader.Read("LINEARRING EMPTY");
            var polygon = reader.Read("POLYGON EMPTY");
            var mpoint = reader.Read("MULTIPOINT EMPTY");
            var mlineString = reader.Read("MULTILINESTRING EMPTY");
            var mpolygon = reader.Read("MULTIPOLYGON EMPTY");
            var gc = reader.Read("GEOMETRYCOLLECTION EMPTY");

            var geometries = new []
            {
                gc,
                mpolygon,
                mlineString,
                mpoint,
                polygon,
                linearRing,
                lineString,
                point
            };

            var geometriesExpectedOrder = new []
            {
                point,
                mpoint,
                lineString,
                linearRing,
                mlineString,
                polygon,
                mpolygon,
                gc
            };

            Array.Sort(geometries);
            for (int i = 0; i < geometries.Length; i++)
                Assert.That(ReferenceEquals(geometries[i], geometriesExpectedOrder[i]), Is.True);
        }

        [Test]
        public void TestPolygonRelate()
        {
            var bigPolygon = reader.Read(
                    "POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            var smallPolygon = reader.Read(
                    "POLYGON ((10 10, 10 30, 30 30, 30 10, 10 10))");
            Assert.IsTrue(bigPolygon.Contains(smallPolygon));
        }

        [Test]
        public void TestEmptyGeometryCentroid()
        {
            Assert.IsTrue(reader.Read("POINT EMPTY").IsEmpty);
            Assert.IsTrue(reader.Read("POLYGON EMPTY").IsEmpty);
            Assert.IsTrue(reader.Read("LINESTRING EMPTY").IsEmpty);
            Assert.IsTrue(reader.Read("GEOMETRYCOLLECTION EMPTY").IsEmpty);
            Assert.IsTrue(reader.Read("GEOMETRYCOLLECTION(GEOMETRYCOLLECTION EMPTY, GEOMETRYCOLLECTION EMPTY)").IsEmpty);
            Assert.IsTrue(reader.Read("MULTIPOLYGON EMPTY").IsEmpty);
            Assert.IsTrue(reader.Read("MULTILINESTRING EMPTY").IsEmpty);
            Assert.IsTrue(reader.Read("MULTIPOINT EMPTY").IsEmpty);
        }

        [Test]
        public void TestNoOutgoingDirEdgeFound()
        {
            doTestFromCommcast2003AtYahooDotCa(reader);
        }

        [Test]
        public void TestOutOfMemoryError()
        {
            doTestFromCommcast2003AtYahooDotCa(new WKTReader());
        }

        [Test]
        public void TestDepthMismatchAssertionFailedException()
        {
            //register@robmeek.com reported an assertion failure
            //("depth mismatch at (160.0, 300.0, Nan)") [Jon Aquino 10/28/2003]
            reader
                .Read("MULTIPOLYGON (((100 300, 100 400, 200 400, 200 300, 100 300)),"
                    + "((160 300, 160 400, 260 400, 260 300, 160 300)),"
                    + "((160 300, 160 200, 260 200, 260 300, 160 300)))").Buffer(0);
        }

        private void doTestFromCommcast2003AtYahooDotCa(WKTReader reader)
        {
            readerFloat.Read(
                "POLYGON ((708653.498611049 2402311.54647056, 708708.895756966 2402203.47250014, 708280.326454234 2402089.6337791, 708247.896591321 2402252.48269854, 708367.379593851 2402324.00761653, 708248.882609455 2402253.07294874, 708249.523621829 2402244.3124463, 708261.854734465 2402182.39086576, 708262.818392579 2402183.35452387, 708653.498611049 2402311.54647056))")
                  .Intersection(reader.Read(
                    "POLYGON ((708258.754920656 2402197.91172757, 708257.029447455 2402206.56901508, 708652.961095455 2402312.65463437, 708657.068786251 2402304.6356364, 708258.754920656 2402197.91172757))"));
        }

        [Ignore("The equalseHash assert for the differentStart geometry is causing a failure in the test.  The problem is caused by a difference in the logic between JTS and NTS.  JTS computes the hash based on the bounding rectangle, which is the same for both shapes, but NTS computes it based on the coordinates in the shape, which is actually delgated to the derived type via an abstract method.  In theory two polygons with the same number of points could have the same bounding rectangle, but have points in different positions - think of a five pointed star, with the inner points in different locations, while the outer points are the same, and therefor defined the bounding rectangle.  On the other hand, two shapes that are equivalent, but have different start points should really have the same hash code.  The logic for GetHashCode on geometries needs to be reviewed before enabling this test again.")]
        public void TestEquals()
        {
            var g = reader.Read("POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            var same = reader.Read("POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            var differentStart = reader.Read(
                    "POLYGON ((0 50, 50 50, 50 0, 0 0, 0 50))");
            var differentFourth = reader.Read(
                    "POLYGON ((0 0, 0 50, 50 50, 50 -99, 0 0))");
            var differentSecond = reader.Read(
                    "POLYGON ((0 0, 0 99, 50 50, 50 0, 0 0))");
            DoTestEquals(g, same, true, true, true, true);
            DoTestEquals(g, differentStart, true, true, false, true);  // NTS casts from object to Geometry if possible, so changed a equalsObject to be true not false
            DoTestEquals(g, differentFourth, false, false, false, false);
            DoTestEquals(g, differentSecond, false, false, false, false);
        }

        private void DoTestEquals(Geometry a, Geometry b, bool equalsGeometry,
            bool equalsObject, bool equalsExact, bool equalsHash)
        {
            Assert.AreEqual(equalsGeometry, a.Equals(b));
            Assert.AreEqual(equalsObject, a.Equals((object) b));
            Assert.AreEqual(equalsExact, a.EqualsExact(b));
            Assert.AreEqual(equalsHash, a.GetHashCode() == b.GetHashCode());
        }

        [Test]
        public void TestInvalidateEnvelope()
        {
            var g = reader.Read("POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            Assert.AreEqual(new Envelope(0, 50, 0, 50), g.EnvelopeInternal);
            g.Apply(new CoordinateFilter());
            Assert.AreEqual(new Envelope(0, 50, 0, 50), g.EnvelopeInternal);
            g.GeometryChanged();
            Assert.AreEqual(new Envelope(1, 51, 1, 51), g.EnvelopeInternal);
        }

        class CoordinateFilter : ICoordinateFilter
        {
            public void Filter(Coordinate coord) {
                coord.X += 1;
                coord.Y += 1;
            }
        }

        [Test]
        public void TestEquals1()
        {
            var polygon1 = reader.Read(
                    "POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            var polygon2 = reader.Read(
                    "POLYGON ((50 50, 50 0, 0 0, 0 50, 50 50))");
            Assert.IsTrue(polygon1.EqualsTopologically(polygon2));
        }

        [Test]
        public void TestEqualsWithNull()
        {
            var polygon = reader.Read("POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            object g = null;
            Assert.IsTrue(!polygon.Equals(g));
        }

        [Ignore("This was commented out in JTS - not sure why")]
        public void TestEquals2()
        {
            //    Geometry lineString = reader.read("LINESTRING(0 0, 0 50, 50 50, 50 0, 0 0)");
            //    Geometry geometryCollection = reader.read("GEOMETRYCOLLECTION ( LINESTRING(0 0  , 0  50), "
            //                                                                 + "LINESTRING(0 50 , 50 50), "
            //                                                                 + "LINESTRING(50 50, 50 0 ), "
            //                                                                 + "LINESTRING(50 0 , 0  0 ) )");
            //    Assert.IsTrue(lineString.equals(geometryCollection));
        }

        [Test]
        public void TestEqualsInHashBasedCollection()
        {
            var p0 = new Coordinate(0, 0);
            var p1 = new Coordinate(0, 1);
            var p2 = new Coordinate(1, 0);

            Coordinate[] exactEqualRing1 = { p0, p1, p2, p0 };
            var exactEqualRing2 = CoordinateArrays.CopyDeep(exactEqualRing1);
            Coordinate[] rotatedRing1 = { p1, p2, p0, p1 };
            Coordinate[] rotatedRing2 = { p2, p0, p1, p2 };

            var exactEqualRing1Poly = geometryFactory.CreatePolygon(exactEqualRing1);
            var exactEqualRing2Poly = geometryFactory.CreatePolygon(exactEqualRing2);
            var rotatedRing1Poly = geometryFactory.CreatePolygon(rotatedRing1);
            var rotatedRing2Poly = geometryFactory.CreatePolygon(rotatedRing2);

            // Geometry equality in hash-based collections should be based on
            // EqualsExact semantics, as it is in JTS.
            var hashSet1 = new HashSet<Geometry>
            {
                exactEqualRing1Poly,
                exactEqualRing2Poly,
                rotatedRing1Poly,
                rotatedRing2Poly,
            };

            Assert.AreEqual(3, hashSet1.Count);

            // same as Polygon equality.
            var hashSet2 = new HashSet<Polygon>
            {
                exactEqualRing1Poly,
                exactEqualRing2Poly,
                rotatedRing1Poly,
                rotatedRing2Poly,
            };

            Assert.AreEqual(3, hashSet2.Count);
        }

        [Test]
        public void TestEqualsExactForLinearRings()
        {
            var x = geometryFactory.CreateLinearRing(new Coordinate[] {
                        new Coordinate(0, 0), new Coordinate(100, 0),
                        new Coordinate(100, 100), new Coordinate(0, 0)
                    });
            var somethingExactlyEqual = geometryFactory.CreateLinearRing(new Coordinate[] {
                        new Coordinate(0, 0), new Coordinate(100, 0),
                        new Coordinate(100, 100), new Coordinate(0, 0)
                    });
            var somethingNotEqualButSameClass = geometryFactory.CreateLinearRing(new Coordinate[] {
                        new Coordinate(0, 0), new Coordinate(100, 0),
                        new Coordinate(100, 555), new Coordinate(0, 0)
                    });
            var sameClassButEmpty = geometryFactory.CreateLinearRing((CoordinateSequence)null);
            var anotherSameClassButEmpty = geometryFactory.CreateLinearRing((CoordinateSequence)null);
            var collectionFactory = new LineCollectionFactory();

            DoTestEqualsExact(x, somethingExactlyEqual,
                somethingNotEqualButSameClass, sameClassButEmpty,
                anotherSameClassButEmpty, collectionFactory);

        //    LineString somethingEqualButNotExactly = geometryFactory.createLineString(new Coordinate[] {
        //          new Coordinate(0, 0), new Coordinate(100, 0), new Coordinate(100, 100),
        //          new Coordinate(0, 0) });
        //
        //    doTestEqualsExact(x, somethingExactlyEqual, somethingEqualButNotExactly,
        //          somethingNotEqualButSameClass);
        }

        [Test]
        public void TestEqualsExactForLineStrings()
        {
            var x = geometryFactory.CreateLineString(new Coordinate[] {
                        new Coordinate(0, 0), new Coordinate(100, 0),
                        new Coordinate(100, 100)
                    });
            var somethingExactlyEqual = geometryFactory.CreateLineString(new Coordinate[] {
                        new Coordinate(0, 0), new Coordinate(100, 0),
                        new Coordinate(100, 100)
                    });
            var somethingNotEqualButSameClass = geometryFactory.CreateLineString(new Coordinate[] {
                        new Coordinate(0, 0), new Coordinate(100, 0),
                        new Coordinate(100, 555)
                    });
            var sameClassButEmpty = geometryFactory.CreateLineString((Coordinate[])null);
            var anotherSameClassButEmpty = geometryFactory.CreateLineString((Coordinate[])null);
            var collectionFactory = new LineCollectionFactory();

            DoTestEqualsExact(x, somethingExactlyEqual,
                somethingNotEqualButSameClass, sameClassButEmpty,
                anotherSameClassButEmpty, collectionFactory);

            var collectionFactory2 = new LineCollectionFactory();

            DoTestEqualsExact(x, somethingExactlyEqual,
                somethingNotEqualButSameClass, sameClassButEmpty,
                anotherSameClassButEmpty, collectionFactory2);
        }

        [Test]
        public void TestEqualsExactForPoints()
        {
            var x = geometryFactory.CreatePoint(new Coordinate(100, 100));
            var somethingExactlyEqual = geometryFactory.CreatePoint(new Coordinate(
                        100, 100));
            var somethingNotEqualButSameClass = geometryFactory.CreatePoint(new Coordinate(
                        999, 100));
            var sameClassButEmpty = geometryFactory.CreatePoint((Coordinate)null);
            var anotherSameClassButEmpty = geometryFactory.CreatePoint((Coordinate)null);
            var collectionFactory = new PointCollectionFactory();

            DoTestEqualsExact(x, somethingExactlyEqual,
                somethingNotEqualButSameClass, sameClassButEmpty,
                anotherSameClassButEmpty, collectionFactory);
        }

        [Test]
        public void TestEqualsExactForPolygons()
        {
            var x = (Polygon) reader.Read(
                    "POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            var somethingExactlyEqual = (Polygon) reader.Read(
                    "POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            var somethingNotEqualButSameClass = (Polygon) reader.Read(
                    "POLYGON ((50 50, 50 0, 0 0, 0 50, 50 50))");
            var sameClassButEmpty = (Polygon) reader.Read("POLYGON EMPTY");
            var anotherSameClassButEmpty = (Polygon) reader.Read(
                    "POLYGON EMPTY");
            var collectionFactory = new PolygonCollectionFactory();

            DoTestEqualsExact(x, somethingExactlyEqual,
                somethingNotEqualButSameClass, sameClassButEmpty,
                anotherSameClassButEmpty, collectionFactory);
        }

        [Test]
        public void TestEqualsExactForGeometryCollections()
        {
            Geometry polygon1 = (Polygon) reader.Read(
                    "POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0))");
            Geometry polygon2 = (Polygon) reader.Read(
                    "POLYGON ((50 50, 50 0, 0 0, 0 50, 50 50))");
            var x = geometryFactory.CreateGeometryCollection(new Geometry[] {
                        polygon1, polygon2
                    });
            var somethingExactlyEqual = geometryFactory.CreateGeometryCollection(new Geometry[] {
                        polygon1, polygon2
                    });
            var somethingNotEqualButSameClass = geometryFactory.CreateGeometryCollection(new Geometry[] {
                        polygon2
                    });
            var sameClassButEmpty = geometryFactory.CreateGeometryCollection(null);
            var anotherSameClassButEmpty = geometryFactory.CreateGeometryCollection(null);
            var collectionFactory = new GeometryCollectionFactory() ;

            DoTestEqualsExact(x, somethingExactlyEqual,
                somethingNotEqualButSameClass, sameClassButEmpty,
                anotherSameClassButEmpty, collectionFactory);
        }

        [Test]
        public void TestGeometryCollectionIntersects1()
        {
            var gc0 = reader.Read("GEOMETRYCOLLECTION ( POINT(0 0) )");
            var gc1 = reader.Read("GEOMETRYCOLLECTION ( LINESTRING(0 0, 1 1) )");
            var gc2 = reader.Read("GEOMETRYCOLLECTION ( LINESTRING(1 0, 0 1) )");

            Assert.IsTrue(gc0.Intersects(gc1));
            Assert.IsTrue(gc1.Intersects(gc2));
            Assert.IsTrue(!gc0.Intersects(gc2));
            // symmetric
            Assert.IsTrue(gc1.Intersects(gc0));
            Assert.IsTrue(gc2.Intersects(gc1));
            Assert.IsTrue(!gc2.Intersects(gc0));
    }

        [Test]
        public void TestGeometryCollectionIntersects2()
        {
            var gc0 = reader.Read("POINT(0 0)");
            var gc1 = reader.Read("GEOMETRYCOLLECTION ( LINESTRING(0 0, 1 1) )");
            var gc2 = reader.Read("LINESTRING(1 0, 0 1)");

            Assert.IsTrue(gc0.Intersects(gc1));
            Assert.IsTrue(gc1.Intersects(gc2));
            // symmetric
            Assert.IsTrue(gc1.Intersects(gc0));
            Assert.IsTrue(gc2.Intersects(gc1));
        }

        [Test]
        public void TestGeometryCollectionIntersects3()
        {
            var gc0 = reader.Read("GEOMETRYCOLLECTION ( POINT(0 0), LINESTRING(1 1, 2 2) )");
            var gc1 = reader.Read("GEOMETRYCOLLECTION ( POINT(15 15) )");
            var gc2 = reader.Read("GEOMETRYCOLLECTION ( LINESTRING(0 0, 2 0), POLYGON((10 10, 20 10, 20 20, 10 20, 10 10)))");

            Assert.IsTrue(gc0.Intersects(gc2));
            Assert.IsTrue(!gc0.Intersects(gc1));
            Assert.IsTrue(gc1.Intersects(gc2));

            // symmetric
            Assert.IsTrue(gc2.Intersects(gc0));
            Assert.IsTrue(!gc1.Intersects(gc0));
            Assert.IsTrue(gc2.Intersects(gc1));
        }

        private void DoTestEqualsExact(Geometry x,
            Geometry somethingExactlyEqual,
            Geometry somethingNotEqualButSameClass,
            Geometry sameClassButEmpty,
            Geometry anotherSameClassButEmpty,
            ICollectionFactory collectionFactory)
        {
            Geometry emptyDifferentClass;

            if (x is Point) {
                emptyDifferentClass = geometryFactory.CreateGeometryCollection(null);
            } else {
                emptyDifferentClass = geometryFactory.CreatePoint((Coordinate)null);
            }

            Geometry somethingEqualButNotExactly = geometryFactory.CreateGeometryCollection(new Geometry[] { x });

            DoTestEqualsExact(x, somethingExactlyEqual,
                collectionFactory.CreateCollection(new Geometry[] { x }, geometryFactory),
                somethingNotEqualButSameClass);

            DoTestEqualsExact(sameClassButEmpty, anotherSameClassButEmpty,
                emptyDifferentClass, x);

            /**
             * Test comparison of non-empty versus empty.
             */
            DoTestEqualsExact(x, somethingExactlyEqual,
                sameClassButEmpty, sameClassButEmpty);

            DoTestEqualsExact(collectionFactory.CreateCollection(
                    new Geometry[] { x, x }, geometryFactory),
                collectionFactory.CreateCollection(
                    new Geometry[] { x, somethingExactlyEqual }, geometryFactory),
                somethingEqualButNotExactly,
                collectionFactory.CreateCollection(
                    new Geometry[] { x, somethingNotEqualButSameClass }, geometryFactory));
        }

        private void DoTestEqualsExact(Geometry x,
            Geometry somethingExactlyEqual,
            Geometry somethingEqualButNotExactly,
            Geometry somethingNotEqualButSameClass)  {
            Geometry differentClass;

            if (x is Point) {
                differentClass = reader.Read(
                        "POLYGON ((0 0, 0 50, 50 43949, 50 0, 0 0))");
            } else {
                differentClass = reader.Read("POINT ( 2351 1563 )");
            }

            Assert.IsTrue(x.EqualsExact(x));
            Assert.IsTrue(x.EqualsExact(somethingExactlyEqual));
            Assert.IsTrue(somethingExactlyEqual.EqualsExact(x));
            Assert.IsTrue(!x.EqualsExact(somethingEqualButNotExactly));
            Assert.IsTrue(!somethingEqualButNotExactly.EqualsExact(x));
            Assert.IsTrue(!x.EqualsExact(somethingEqualButNotExactly));
            Assert.IsTrue(!somethingEqualButNotExactly.EqualsExact(x));
            Assert.IsTrue(!x.EqualsExact(differentClass));
            Assert.IsTrue(!differentClass.EqualsExact(x));
        }

        private interface ICollectionFactory {
            Geometry CreateCollection(Geometry[] geometries, GeometryFactory geometryFactory);
        }

        class GeometryCollectionFactory : ICollectionFactory
        {
            public Geometry CreateCollection(Geometry[] geometries, GeometryFactory geometryFactory) {
                return geometryFactory.CreateGeometryCollection(geometries);
            }
        }

        class PointCollectionFactory : ICollectionFactory
        {
            public Geometry CreateCollection(Geometry[] geometries, GeometryFactory geometryFactory) {
                return geometryFactory.CreateMultiPoint(GeometryFactory.ToPointArray(geometries));
            }
        }

        class LineCollectionFactory : ICollectionFactory
        {
            public Geometry CreateCollection(Geometry[] geometries, GeometryFactory geometryFactory) {
                return geometryFactory.CreateMultiLineString(GeometryFactory.ToLineStringArray(geometries));
            }
        }

        class PolygonCollectionFactory : ICollectionFactory
        {
            public Geometry CreateCollection(Geometry[] geometries, GeometryFactory geometryFactory) {
                return geometryFactory.CreateMultiPolygon(GeometryFactory.ToPolygonArray(geometries));
            }
        }

    }
}
