﻿using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;

namespace GreySMITH.Revit.Commands.Extensions.Documents
{
    public static class ElementUtils
    {
        /// <summary>
        /// Does this work yet?
        /// </summary>
        /// <param name="curdoc"></param>
        /// <param name="faminstance"></param>
        /// <returns></returns>
        public static Element FindHost(this Document curdoc, FamilyInstance faminstance )
        {
            Element host = null;

            #region Find the Revit Link*
            // get the revitlinkinstances from the current document
            FilteredElementCollector collectionofRVTInst = new FilteredElementCollector(curdoc).OfCategory(BuiltInCategory.OST_RvtLinks);

            // selects the link which matches the instance's host's document's path
            RevitLinkInstance parentlinkDoc = (from posslink in collectionofRVTInst
                                               where (posslink as RevitLinkInstance).GetLinkDocument().PathName.ToString().Equals(faminstance.Host.Document.PathName.ToString())
                                               select (posslink as RevitLinkInstance)).Single();
            #endregion

            #region Finding the Host in the Linked Document
            // for face based elements
            // make a list of elements in the linked document which match the host's type
            // in this test case, these should return walls
            FilteredElementCollector linkdocfec = new FilteredElementCollector(faminstance.Host.Document);
            linkdocfec.OfClass(faminstance.Host.GetType());

            // find the host in the list by comparing the UNIQUEIDS
            host = (from posshost in linkdocfec
                    where posshost.UniqueId.ToString().Equals(faminstance.Host.UniqueId.ToString())
                    select posshost).First();
            #endregion

            return host;
        }

        public static Element FindElementinLinkedDoc(this Document curDoc, string elementUNIQUEID)
        {
            Element element = null;

            #region Find the Revit Link*
            // get the revitlinks from the current document
            FilteredElementCollector collectionofRVTInst = new FilteredElementCollector(curDoc).OfCategory(BuiltInCategory.OST_RvtLinks);

            #endregion

            #region Finding the Element in the Linked Document
            // convert those objects into RevitLinkInstances?
            //IEnumerable<RevitLinkInstance> parentlinkDoc = from posslink in collectionofRVTInst
            //                                               where (posslink is RevitLinkInstance)
            //                                               select posslink as RevitLinkInstance;

            // check each of the documents to see if they contain the element
            // select appropiate link
            RevitLinkInstance rvtlink = (from posslink in collectionofRVTInst
                                         where (posslink as RevitLinkInstance).GetLinkDocument().GetElement(elementUNIQUEID).IsValidObject
                                         select posslink as RevitLinkInstance).FirstOrDefault();

            // find the host in the list by comparing the UNIQUEIDS
            element = rvtlink.GetLinkDocument().GetElement(elementUNIQUEID);
            #endregion

            return element;
        }

        public static Element FindHost(this Document curDoc, Element elem)
        {
            Element host = null;

            // if the element is actually a family instance
            if (elem is FamilyInstance)
            {
                host = curDoc.FindHost(elem);
            }

            // however, if the element is a family symbol
            else
            {
                string msg = "The object which was used is a FamilySymbol. Use a FamilyInstance instead.";
                Debug.WriteLine(msg);
                throw new Exception(msg);
            }

            return host;
        }

        /// <summary>
        /// Offsets an element a set amount in a specific direction set by the XYZDirection enum
        /// </summary>
        /// <param name="originalelement">element, the new element should be offset from</param>
        /// <param name="offsetamount">amount new element should be offset from original</param>
        /// <param name="offsetdirection">direction element should be offset</param>
        /// <returns></returns>
        public static ElementId Offset(this Element originalelement, double offsetamount, XYZDirection offsetdirection)
        {
            // variable for new offset element
            ElementId newelement = null;

            // the current document
            Document curdoc = originalelement.Document;

            // element's XYZ
            LocationPoint elp = originalelement.Location as LocationPoint;
            XYZ elem_location = null;
            

            // depending on user input offset direction should be incremented by the offsetamount
            switch(offsetdirection)
            {
                default:
                    break;

                case XYZDirection.X:
                    elem_location = new XYZ(offsetamount, 0.0, 0.0) + elp.Point;
                    break;

                case XYZDirection.Y:
                    elem_location = new XYZ(0.0, offsetamount, 0.0) + elp.Point;
                    break;

                case XYZDirection.Z:
                    elem_location = new XYZ(0.0, 0.0, offsetamount) + elp.Point;
                    break;
            }

            try
            {
                // attempt to offset the element within the transaction
                using (Transaction tr_offset = new Transaction(curdoc, "Offsetting element"))
                {
                    tr_offset.Start();
                    newelement = ElementTransformUtils.CopyElement(curdoc, originalelement.Id, elem_location).FirstOrDefault();
                    tr_offset.Commit();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Command Failed. See below: \n" + e.StackTrace.ToString());
            }

            return newelement;
        }
    }
}
