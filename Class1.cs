using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using LicenseManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace background
{
    public class Class1
    {
        [CommandMethod("NSVbackground")]
        public static void TestDialog()
        {


            var lc = LicenseManager.LicenseChecker.checkLicense(
                "f973f8f0-3087-4140-853c-215c0499b192", false, false);
            if (!lc)
                return;

            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            var psr = ed.SelectAll();
            

            // Prompt the user for the layer name
            var layerNames = GetLayerNames(db);
            string layerName = null;
            using (var dialog = new LayerList("Select layer", layerNames))
            {
                if (AcAp.ShowModalDialog(dialog) != DialogResult.OK)
                    return;
                layerName = dialog.SelectedItem;
            }

            ChangeBlockDef(db,ed);

            // Process the selection
            SetLayer(layerName, psr.Value);
        }

        private static void ChangeBlockDef(Database db,Editor ed)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (!(btr.IsLayout || btr.IsDependent || btr.IsFromExternalReference || btr.IsFromOverlayReference))
                    {
                        foreach (ObjectId id in btr)
                        {
                            try
                            {
                                var entity = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                                if (entity is BlockReference)
                                {
                                    var blk = (BlockReference)entity;
                                    if (blk.BlockTableRecord.IsNull)
                                    {
                                        ed.WriteMessage("\nBlock Reference {0} has a null BlockTableRecord" 
                                            + blk.Handle.ToString());
                                        continue;
                                    }
                                }

                                entity.Layer = "0";
                                entity.ColorIndex = 0; // ByBlock
                                if (entity is BlockReference br)
                                {
                                    foreach (ObjectId attId in br.AttributeCollection)
                                    {
                                        var attrib = (AttributeReference)tr.GetObject(attId, OpenMode.ForWrite);
                                        attrib.Layer = "0";
                                        attrib.ColorIndex = 0; // ByBlock
                                        attrib.Draw();
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {

                                ed.WriteMessage("\nError in changing block definition layer"
                                    + ex.Message);
                            }
                        }
                    }
                }
                tr.Commit();
                ed.Regen();
            }
        }

        static void SetLayer(string layerName, SelectionSet selection)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Unlock all layers
                var lockedLayers = new List<LayerTableRecord>();
                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (layer.IsLocked)
                    {
                        tr.GetObject(id, OpenMode.ForWrite);
                        layer.IsLocked = false;
                        lockedLayers.Add(layer);
                    }
                }

                // Process the selection set
                var processed = new HashSet<string>();

                using (var pm = new ProgressMeter())
                {
                    pm.Start("Working on file...");
                    pm.SetLimit(selection.Count);

                    foreach (ObjectId id in selection.GetObjectIds())
                    {
                        pm.MeterProgress();
                        System.Windows.Forms.Application.DoEvents();

                        var entity = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        entity.Layer = layerName;
                        entity.ColorIndex = 256;
                        entity.LineWeight = LineWeight.ByLayer;
                        // if the entity is a block reference
                        if (entity is BlockReference br)
                        {
                            SetBlockByLayer(br, layerName, processed, tr);
                        }
                        else if (entity is RotatedDimension rdm)
                        {
                            //this changes the text https://cadtips.cadalyst.com/dimension/add-colors-dimensions
                            rdm.Dimclrt = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                            //this changes the line color
                            rdm.Dimclrd = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                            //this changes the border color
                            rdm.Dimclre = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                        }
                        else if(entity is Dimension dm)
                        {
                            //this changes the text https://cadtips.cadalyst.com/dimension/add-colors-dimensions
                            dm.Dimclrt = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                            //this changes the line color
                            dm.Dimclrd = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                            //this changes the border color
                            dm.Dimclre = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                        }
                        
                    }
                }
                // Relock previouly locked layers
                foreach (var layer in lockedLayers)
                {
                    layer.IsLocked = true;
                }
                tr.Commit();
            }
        }
        static void SetBlockByLayer(BlockReference blockRef, string layerName, HashSet<string> processed, Transaction tr)
        {
            var btr = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);
            if (processed.Add(btr.Name))
            {
                foreach (ObjectId id in btr)
                {
                   
                    var entity = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    if (entity is Dimension)
                    {
                        
                        var dm = (Dimension)entity;

                        dm.ColorIndex = 0;

                        dm.SetDatabaseDefaults();
                        //this changes the text https://cadtips.cadalyst.com/dimension/add-colors-dimensions
                        dm.Dimclrt = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                        //this changes the line color
                        dm.Dimclrd = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                        //this changes the border color
                        dm.Dimclre = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                    }
                    else if (entity is Leader)
                    {
                        var leader = (Leader)entity;
                        leader.Dimclrd = Color.FromColorIndex(ColorMethod.ByLayer, 0);
                    }
                    else if(entity is RotatedDimension rdm)
                    {
                        rdm.ColorIndex = 0;
                        
                    }

                    if (entity is BlockReference br)
                    {
                        SetBlockByLayer(br, layerName, processed, tr);
                    }
                    else if(entity is Hatch hatch)
                    {
                        hatch.HatchObjectType = HatchObjectType.HatchObject;
                    }
                    entity.Layer = layerName;
                    entity.ColorIndex = 256;
                    entity.LineWeight = LineWeight.ByLayer;
                }
            }
        }

        static HashSet<string> GetLayerNames(Database db)
        {
            var names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(layer.Name);
                }
                tr.Commit();
            }
            return names;
        }

    }
}