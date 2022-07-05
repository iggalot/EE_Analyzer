using Autodesk.AutoCAD.EditorInput;
using EE_Analyzer.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace EE_Analyzer
{
    public enum UIModes
    {
        MODE_X_DIR_UNDEFINED = -2,
        MODE_Y_DIR_UNDEFINED = -1,
        MODE_X_DIR_SPA      = 0,
        MODE_X_DIR_QTY      = 1,
        MODE_X_DIR_DETAIL   = 2,
        MODE_Y_DIR_SPA      = 3,
        MODE_Y_DIR_QTY      = 4,
        MODE_Y_DIR_DETAIL   = 5
    }


    /// <summary>
    /// Interaction logic for EE_FDNInputDialog.xaml
    /// </summary>
    public partial class EE_FDNInputDialog : Window
    {
        // signals that the mode will use the quantity and spacings as the same for all beams
        public UIModes UI_MODE_X_DIR { get; set; }
        public UIModes UI_MODE_Y_DIR { get; set; }

        public FoundationLayout FDNLayout { get; set; } = null;

        public static string VERSION_INFO { get; } = EE_Settings.CURRENT_VERSION_NUM;
        public static string COPYRIGHT_INFO { get; } = EE_Settings.SIGNATURE_LABEL;

        private int x_qty = 5;
        private double x_spa = 120;
        private double x_depth = 24;
        private double x_width = 12;
        private int beam_x_strand_qty = 2;
        private int slab_x_strand_qty = 0;
        private int y_qty = 5;
        private double y_spa = 120;
        private double y_depth = 24;
        private double y_width = 12;
        private int beam_y_strand_qty = 2;
        private int slab_y_strand_qty = 0;
        private double neglect_pt_dim = 120;
        private int x_spa_1_qty = 3;
        private int x_spa_2_qty = 0;
        private int x_spa_3_qty = 0;
        private int x_spa_4_qty = 0;
        private int x_spa_5_qty = 0;
        private double x_spa_1_spa = 50;
        private double x_spa_2_spa = 0;
        private double x_spa_3_spa = 0;
        private double x_spa_4_spa = 0;
        private double x_spa_5_spa = 0;
        private int y_spa_1_qty = 3;
        private int y_spa_2_qty = 0;
        private int y_spa_3_qty = 0;
        private int y_spa_4_qty = 0;
        private int y_spa_5_qty = 0;
        private double y_spa_1_spa = 50;
        private double y_spa_2_spa = 0;
        private double y_spa_3_spa = 0;
        private double y_spa_4_spa = 0;
        private double y_spa_5_spa = 0;

        private bool piers_is_checked = false;
        private double pier_width = 12;
        private double pier_height = 12;
        private PierShapes pier_shape = PierShapes.PIER_UNDEFINED;

        private bool preview_mode = true;
        public bool dialog_should_close = false;
        public bool dialog_is_complete = false;

        public EE_FDNInputDialog(
            FoundationLayout fdn_layout,
            int x_qty = 5, double x_spa = 120, double x_width = 12, double x_depth = 24,
            int y_qty = 7, double y_spa = 120, double y_width = 12, double y_depth = 24,
            int beam_x_strand_qty = 2, int slab_x_strand_qty = 8, int beam_y_strand_qty = 2, int slab_y_strand_qty = 8, double neglect_pt_dim = 240,
            int x_spa_1_qty = 3, int x_spa_2_qty = 0, int x_spa_3_qty = 0, int x_spa_4_qty = 0, int x_spa_5_qty = 0,
            double x_spa_1_spa = 50, double x_spa_2_spa = 0, double x_spa_3_spa = 0, double x_spa_4_spa = 0, double x_spa_5_spa = 0,
            int y_spa_1_qty = 3, int y_spa_2_qty = 0, int y_spa_3_qty = 0, int y_spa_4_qty = 0, int y_spa_5_qty = 0,
            double y_spa_1_spa = 50, double y_spa_2_spa = 0, double y_spa_3_spa = 0, double y_spa_4_spa = 0, double y_spa_5_spa = 0,
            bool piers_specified = false, PierShapes pier_shape = PierShapes.PIER_UNDEFINED, double pier_width = 12.0, double pier_height = 12.0,
            bool should_close = false
            )
        {
            InitializeComponent();

            FDNLayout = fdn_layout;

            DataContext = this;

            BEAM_X_QTY.Text = x_qty.ToString();
            BEAM_X_SPACING.Text = x_spa.ToString();
            BEAM_X_DEPTH.Text = x_depth.ToString();
            BEAM_X_WIDTH.Text = x_width.ToString();
            BEAM_X_STRAND_QTY.Text = beam_x_strand_qty.ToString();
            SLAB_X_STRAND_QTY.Text = slab_x_strand_qty.ToString();
                        
            BEAM_Y_QTY.Text = y_qty.ToString();
            BEAM_Y_SPACING.Text = y_spa.ToString();
            BEAM_Y_DEPTH.Text = y_depth.ToString();
            BEAM_Y_WIDTH.Text = y_width.ToString();
            BEAM_Y_STRAND_QTY.Text = beam_y_strand_qty.ToString();
            SLAB_Y_STRAND_QTY.Text = slab_y_strand_qty.ToString();

            NEGLECT_PT_DIM.Text = EE_Settings.DEFAULT_MIN_PT_LENGTH.ToString();

            X_SPA_1_QTY.Text = x_spa_1_qty.ToString();
            X_SPA_2_QTY.Text = x_spa_2_qty.ToString();
            X_SPA_3_QTY.Text = x_spa_3_qty.ToString();
            X_SPA_4_QTY.Text = x_spa_4_qty.ToString();
            X_SPA_5_QTY.Text = x_spa_5_qty.ToString();
            X_SPA_1_SPA.Text = x_spa_1_spa.ToString();
            X_SPA_2_SPA.Text = x_spa_2_spa.ToString();
            X_SPA_3_SPA.Text = x_spa_3_spa.ToString();
            X_SPA_4_SPA.Text = x_spa_4_spa.ToString();
            X_SPA_5_SPA.Text = x_spa_5_spa.ToString();

            Y_SPA_1_QTY.Text = y_spa_1_qty.ToString();
            Y_SPA_2_QTY.Text = y_spa_2_qty.ToString();
            Y_SPA_3_QTY.Text = y_spa_3_qty.ToString();
            Y_SPA_4_QTY.Text = y_spa_4_qty.ToString();
            Y_SPA_5_QTY.Text = y_spa_5_qty.ToString();
            Y_SPA_1_SPA.Text = y_spa_1_spa.ToString();
            Y_SPA_2_SPA.Text = y_spa_2_spa.ToString();
            Y_SPA_3_SPA.Text = y_spa_3_spa.ToString();
            Y_SPA_4_SPA.Text = y_spa_4_spa.ToString();
            Y_SPA_5_SPA.Text = y_spa_5_spa.ToString();

            UI_MODE_X_DIR = UIModes.MODE_X_DIR_UNDEFINED;
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_UNDEFINED;

            dialog_should_close = should_close;

            // Populate our pier shape information
            chPiersActive.IsChecked = piers_specified;
            PIER_DIA.Text = pier_width.ToString();
            PIER_HT.Text = pier_height.ToString();

            cbPierShape.Items.Add("Circular");
            cbPierShape.Items.Add("Rectangular");
            cbPierShape.SelectedItem="Circular";
        }

        /// <summary>
        /// Parses the form data.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool ParseFormData()
        {
            bool resultOK = true;
            // Grab the values from the dialog and check for validity
                if (Double.TryParse(BEAM_X_DEPTH.Text, out x_depth) is false)
                {

                    MessageBox.Show("Error reading X-dir beam depth");
                    resultOK = false;
                }
                if (Double.TryParse(BEAM_X_WIDTH.Text, out x_width) is false)
                {
                    MessageBox.Show("Error reading X-dir beam width");
                    resultOK = false;
                }
                if (Double.TryParse(NEGLECT_PT_DIM.Text, out neglect_pt_dim) is false)
                {
                    MessageBox.Show("Error reading minimum length of PT");
                    resultOK = false;
                }

                if (Int32.TryParse(BEAM_X_STRAND_QTY.Text, out beam_x_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading X-dir beam strand qty");
                }
                if (Int32.TryParse(BEAM_Y_STRAND_QTY.Text, out beam_y_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading Y-dir beam strand qty");
                }
                if (Int32.TryParse(SLAB_X_STRAND_QTY.Text, out slab_x_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading X-dir slab strand qty");
                }
                if (Int32.TryParse(SLAB_Y_STRAND_QTY.Text, out slab_y_strand_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading Y-dir slab strand qty");
                }


                if (UI_MODE_X_DIR == UIModes.MODE_X_DIR_QTY)
                {
                    if (Int32.TryParse(BEAM_X_QTY.Text, out x_qty) is false)
                    {
                        resultOK = false;
                        MessageBox.Show("Error reading X-dir beam qty");
                    }

                }
                else if (UI_MODE_X_DIR == UIModes.MODE_X_DIR_SPA)
                {
                    if (Double.TryParse(BEAM_X_SPACING.Text, out x_spa) is false)
                    {
                        MessageBox.Show("Error reading X-dir beam spacing");
                        resultOK = false;
                    }
                }
                else if (UI_MODE_X_DIR == UIModes.MODE_X_DIR_DETAIL)
                {
                    if (Int32.TryParse(X_SPA_1_QTY.Text, out x_spa_1_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_1 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_2_QTY.Text, out x_spa_2_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_2 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_3_QTY.Text, out x_spa_3_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_3 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_4_QTY.Text, out x_spa_4_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_4 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(X_SPA_5_QTY.Text, out x_spa_5_qty) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_5 quantity");
                        resultOK = false;
                    }

                    if (Double.TryParse(X_SPA_1_SPA.Text, out x_spa_1_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_1 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_2_SPA.Text, out x_spa_2_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_2 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_3_SPA.Text, out x_spa_3_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_3 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_4_SPA.Text, out x_spa_4_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_4 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(X_SPA_5_SPA.Text, out x_spa_5_spa) is false)
                    {
                        MessageBox.Show("Error reading X_SPA_5 spacing");
                        resultOK = false;
                    }
                }
                else
                {
                    throw new NotImplementedException("Invalid Mode in X-Dir");
                }

                // Y-direction modes
                if (Double.TryParse(BEAM_Y_DEPTH.Text, out y_depth) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam spacing");
                    resultOK = false;
                }
                if (Double.TryParse(BEAM_Y_WIDTH.Text, out y_width) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam spacing");
                    resultOK = false;
                }

                if (UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_QTY)
                {
                    if (Int32.TryParse(BEAM_Y_QTY.Text, out y_qty) is false)
                    {
                        MessageBox.Show("Error reading Y-dir beam qty");
                        resultOK = false;
                    }
                }
                else if (UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_SPA)
                {
                    if (Double.TryParse(BEAM_Y_SPACING.Text, out y_spa) is false)
                    {
                        MessageBox.Show("Error reading Y-dir beam spcing");
                        resultOK = false;
                    }
                }
                else if (UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_DETAIL)
                {
                    if (Int32.TryParse(Y_SPA_1_QTY.Text, out y_spa_1_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_1 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_2_QTY.Text, out y_spa_2_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_2 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_3_QTY.Text, out y_spa_3_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_3 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_4_QTY.Text, out y_spa_4_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_4 quantity");
                        resultOK = false;
                    }
                    if (Int32.TryParse(Y_SPA_5_QTY.Text, out y_spa_5_qty) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_5 quantity");
                        resultOK = false;
                    }

                    if (Double.TryParse(Y_SPA_1_SPA.Text, out y_spa_1_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_1 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_2_SPA.Text, out y_spa_2_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_2 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_3_SPA.Text, out y_spa_3_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_3 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_4_SPA.Text, out y_spa_4_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_4 spacing");
                        resultOK = false;
                    }
                    if (Double.TryParse(Y_SPA_5_SPA.Text, out y_spa_5_spa) is false)
                    {
                        MessageBox.Show("Error reading Y_SPA_5 spacing");
                        resultOK = false;
                    }
                }
                else
                {
                    throw new NotImplementedException("Invalid Mode in X-Dir");
                }

                // Parse the piers input
                if (chPiersActive.IsChecked == true)
                {
                    piers_is_checked = true;
                    switch (cbPierShape.SelectedItem)
                    {
                        case "Circular":
                            if (Double.TryParse(PIER_DIA.Text, out pier_width) is false)
                            {
                                MessageBox.Show("Error reading Pier Diameter for circular piers");
                                resultOK = false;
                            }
                            pier_height = 0;
                            pier_shape = PierShapes.PIER_CIRCLE;
                            break;
                        case "Rectangular":
                            if (Double.TryParse(PIER_DIA.Text, out pier_width) is false)
                            {
                                MessageBox.Show("Error reading Pier width for rectangular piers");
                                resultOK = false;
                            }
                            if (Double.TryParse(PIER_HT.Text, out pier_height) is false)
                            {
                                MessageBox.Show("Error reading Pier height for rectangular piers");
                                resultOK = false;
                            }
                            pier_shape = PierShapes.PIER_RECTANGLE;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    pier_shape = PierShapes.PIER_UNDEFINED;
                    pier_height = 0;
                    pier_width = 0;
                    piers_is_checked = false;
                }

                return resultOK;
            }
            
        /// <summary>
        /// Update the Foundation Layour data
        /// </summary>
        private void StoreFormData()
        {
            FDNLayout.PreviewMode = preview_mode;
            FDNLayout.ShouldClose = dialog_should_close;

            EE_Settings.DEFAULT_MIN_PT_LENGTH = neglect_pt_dim;

            FDNLayout.PiersSpecified = piers_is_checked;
            FDNLayout.PierShape = pier_shape;
            FDNLayout.PierWidth = pier_width;
            FDNLayout.PierHeight = pier_height;

            FDNLayout.Beam_X_Spacing = x_spa;  // spacing between horizontal beams
            FDNLayout.Beam_X_Width = x_width;  // horizontal beam width
            FDNLayout.Beam_X_Depth = x_depth;  // horizontal beam depth
            FDNLayout.Beam_X_Qty = x_qty;         // horizontal beam qty
            FDNLayout.Beam_X_Strand_Qty = beam_x_strand_qty;  // number of strands in each x-direction beam
            FDNLayout.Beam_X_Slab_Strand_Qty = slab_x_strand_qty;  // number of strands in x-direction slab

            FDNLayout.Beam_Y_Spacing = y_spa;  // spacing between vertical beams
            FDNLayout.Beam_Y_Width = y_width;  // vertical beam width
            FDNLayout.Beam_Y_Depth = y_depth;  // vertical beam depth
            FDNLayout.Beam_Y_Qty = y_qty;         // vertical beam qty 
            FDNLayout.Beam_Y_Strand_Qty = beam_y_strand_qty;  // number of strands in each y-direction beam
            FDNLayout.Beam_Y_Slab_Strand_Qty = slab_y_strand_qty;  // number of strands in y-direction slab

            FDNLayout.Beam_X_DETAIL_QTY_1 = x_spa_1_qty;
            FDNLayout.Beam_X_DETAIL_QTY_2 = x_spa_2_qty;
            FDNLayout.Beam_X_DETAIL_QTY_3 = x_spa_3_qty;
            FDNLayout.Beam_X_DETAIL_QTY_4 = x_spa_4_qty;
            FDNLayout.Beam_X_DETAIL_QTY_5 = x_spa_5_qty;

            FDNLayout.Beam_X_DETAIL_SPA_1 = x_spa_1_spa;
            FDNLayout.Beam_X_DETAIL_SPA_2 = x_spa_2_spa;
            FDNLayout.Beam_X_DETAIL_SPA_3 = x_spa_3_spa;
            FDNLayout.Beam_X_DETAIL_SPA_4 = x_spa_4_spa;
            FDNLayout.Beam_X_DETAIL_SPA_5 = x_spa_5_spa;

            FDNLayout.Beam_Y_DETAIL_QTY_1 = y_spa_1_qty;
            FDNLayout.Beam_Y_DETAIL_QTY_2 = y_spa_2_qty;
            FDNLayout.Beam_Y_DETAIL_QTY_3 = y_spa_3_qty;
            FDNLayout.Beam_Y_DETAIL_QTY_4 = y_spa_4_qty;
            FDNLayout.Beam_Y_DETAIL_QTY_5 = y_spa_5_qty;

            FDNLayout.Beam_Y_DETAIL_SPA_1 = y_spa_1_spa;
            FDNLayout.Beam_Y_DETAIL_SPA_2 = y_spa_2_spa;
            FDNLayout.Beam_Y_DETAIL_SPA_3 = y_spa_3_spa;
            FDNLayout.Beam_Y_DETAIL_SPA_4 = y_spa_4_spa;
            FDNLayout.Beam_Y_DETAIL_SPA_5 = y_spa_5_spa;
        }

        /// <summary>
        /// Handles the 'Click' event of the 'OK' button.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event data.</param>
        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            preview_mode = true;
            dialog_should_close = false;

            try { 
                if (ParseFormData())
                {
                    // write the form data to the foundation layout object
                    StoreFormData();

                    FDNLayout.DrawFoundationDetails(
                        x_qty, x_spa, x_depth, x_width,
                        y_qty, y_spa, y_depth, y_width,
                        beam_x_strand_qty, slab_x_strand_qty, beam_y_strand_qty, slab_y_strand_qty,
                        x_spa_1_qty, x_spa_2_qty, x_spa_3_qty, x_spa_4_qty, x_spa_5_qty,
                        x_spa_1_spa, x_spa_2_spa, x_spa_3_spa, x_spa_4_spa, x_spa_5_spa,
                        y_spa_1_qty, y_spa_2_qty, y_spa_3_qty, y_spa_4_qty, y_spa_5_qty,
                        y_spa_1_spa, y_spa_2_spa, y_spa_3_spa, y_spa_4_spa, y_spa_5_spa, 
                        UI_MODE_X_DIR, UI_MODE_Y_DIR, 
                        piers_is_checked,  pier_shape, pier_width, pier_height,
                        preview_mode, dialog_should_close,
                        neglect_pt_dim 
                        );

                    FDNLayout.IsComplete = dialog_should_close;
                }
                else
                {
                    MessageBox.Show("Error parsing dialog window values");
                    return;
                }

            } catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
                return;
            }
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

            preview_mode = false;
            dialog_should_close = true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            preview_mode = false; // turn off preview mode so final drawings can be completed.
            dialog_should_close = true;
            try
            {
                if (ParseFormData())
                {
                    // write the form data to the foundation layout object
                    StoreFormData();

                    FDNLayout.DrawFoundationDetails(
                        x_qty, x_spa, x_depth, x_width,
                        y_qty, y_spa, y_depth, y_width,
                        beam_x_strand_qty, slab_x_strand_qty, beam_y_strand_qty, slab_y_strand_qty,
                        x_spa_1_qty, x_spa_2_qty, x_spa_3_qty, x_spa_4_qty, x_spa_5_qty,
                        x_spa_1_spa, x_spa_2_spa, x_spa_3_spa, x_spa_4_spa, x_spa_5_spa,
                        y_spa_1_qty, y_spa_2_qty, y_spa_3_qty, y_spa_4_qty, y_spa_5_qty,
                        y_spa_1_spa, y_spa_2_spa, y_spa_3_spa, y_spa_4_spa, y_spa_5_spa,
                        UI_MODE_X_DIR, UI_MODE_Y_DIR,
                        piers_is_checked, pier_shape, pier_width, pier_height,
                        false, false,
                        neglect_pt_dim
                        );

                    // now set the flag to close the dialog
                    preview_mode = false;
                    dialog_should_close = true;
                    dialog_is_complete = true;

                }
                else
                {
                    MessageBox.Show("Error parsing dialog window values");
                    return;
                }

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
                return;
            }

            FDNLayout.PreviewMode = false;
            FDNLayout.IsComplete = dialog_is_complete;
            FDNLayout.PreviewMode = preview_mode;
            this.DialogResult = false;

        }

        private void UpdateOKButton()
        {
            if(UI_MODE_X_DIR == UIModes.MODE_X_DIR_UNDEFINED || UI_MODE_Y_DIR == UIModes.MODE_Y_DIR_UNDEFINED)
            {
                return;
            } else
            {
                btnOK.IsEnabled = true;
                btnOK.Visibility = Visibility.Visible;
            }
            start.InvalidateArrange();
        }

        private void X_Detail_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_X_DIR = UIModes.MODE_X_DIR_DETAIL;
            btnX_Specify_Detail_Spacings.Background = Brushes.MediumOrchid;
            btnX_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnX_Detail_Qty.Background = Brushes.White;

            spX_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spX_MAX_QTY.Visibility = Visibility.Collapsed;
            spX_DIR_DETAILS.Visibility = Visibility.Visible;

            UpdateOKButton();
        }

        private void X_Detail_Qty_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_X_DIR = UIModes.MODE_X_DIR_QTY;
            btnX_Specify_Detail_Spacings.Background = Brushes.White;
            btnX_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnX_Detail_Qty.Background = Brushes.MediumOrchid;

            spX_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spX_MAX_QTY.Visibility = Visibility.Visible;
            spX_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();
        }

        private void X_Detail_Spa_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_X_DIR = UIModes.MODE_X_DIR_SPA;
            btnX_Specify_Detail_Spacings.Background = Brushes.White;
            btnX_Detail_Uniform_Max_Spacing.Background = Brushes.MediumOrchid;
            btnX_Detail_Qty.Background = Brushes.White;

            spX_UNIFORM_MAX_SPA.Visibility = Visibility.Visible;
            spX_MAX_QTY.Visibility = Visibility.Collapsed;
            spX_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();
            UpdateOKButton();

        }

        private void Y_Detail_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_DETAIL;
            btnY_Specify_Detail_Spacings.Background = Brushes.MediumOrchid;
            btnY_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnY_Detail_Qty.Background = Brushes.White;

            spY_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spY_MAX_QTY.Visibility = Visibility.Collapsed;
            spY_DIR_DETAILS.Visibility = Visibility.Visible;

            UpdateOKButton();
        }

        private void Y_Detail_Qty_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_QTY;
            btnY_Specify_Detail_Spacings.Background = Brushes.White;
            btnY_Detail_Uniform_Max_Spacing.Background = Brushes.White;
            btnY_Detail_Qty.Background = Brushes.MediumOrchid;

            spY_UNIFORM_MAX_SPA.Visibility = Visibility.Collapsed;
            spY_MAX_QTY.Visibility = Visibility.Visible;
            spY_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();

        }

        private void Y_Detail_Spa_Button_Click(object sender, RoutedEventArgs e)
        {
            UI_MODE_Y_DIR = UIModes.MODE_Y_DIR_SPA;
            btnY_Specify_Detail_Spacings.Background = Brushes.White;
            btnY_Detail_Uniform_Max_Spacing.Background = Brushes.MediumOrchid;
            btnY_Detail_Qty.Background = Brushes.White;

            spY_UNIFORM_MAX_SPA.Visibility = Visibility.Visible;
            spY_MAX_QTY.Visibility = Visibility.Collapsed;
            spY_DIR_DETAILS.Visibility = Visibility.Collapsed;

            UpdateOKButton();
        }

        private void Piers_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (chPiersActive.IsChecked == true)
            {
                spPierInputData.Visibility = Visibility.Visible;
            }
            else
            {
                spPierInputData.Visibility = Visibility.Collapsed;
            }

        }

        private void cbPierShape_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            if(combo.SelectedItem == "Circular")
            {
                spPierRectangleData.Visibility = Visibility.Collapsed;
            }
            else
            {
                spPierRectangleData.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Needed to help the window start in the upper left corner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void start_Loaded(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(100);
            Window win = sender as Window;
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Top = 0;
            win.Left = 0;
 //           win.Show();
        }

    }
}
