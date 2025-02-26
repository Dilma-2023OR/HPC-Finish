using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HPC_Finish.RuncardWebService;
using System.Deployment.Application;
using System.Collections;

namespace HPC_Finish
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            //Get Publish version
            //Version ver = ApplicationDeployment.CurrentDeployment.CurrentVersion;
            //lblVersion.Text = ver.Major + "." + ver.Minor + "." + ver.Build + "." + ver.Revision;
        }

        //Config Connection
        INIFile localConfig = new INIFile(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\HPC Finish\config.ini");

        //Runcard Connection
        runcard_wsdlPortTypeClient client = new runcard_wsdlPortTypeClient("runcard_wsdlPort");
        string msg = string.Empty;
        unitBOM[] getBOM = null;
        int error = 0;

        //Config Data
        string warehouseBin = string.Empty;
        string warehouseLoc = string.Empty;
        string partClass = string.Empty;
        string machineId = string.Empty;
        string opcode = string.Empty;
        string seqnum = string.Empty;

        string [] Donnorserial = new string[2];
        string [] DonnorpartNum = new string[2];
        string[] Donnorparrev = new string[2];

        int cont = 0;
        int contEtiquetas = 0;
        string textoDespuesDelEspacio = string.Empty;
        private void Main_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(localConfig.FilePath)))
                {
                    //Config Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(localConfig.FilePath));
                    File.Copy(Directory.GetCurrentDirectory() + "\\config.ini", localConfig.FilePath);
                }

                //Save Config Data
                warehouseBin = localConfig.Read("RUNCARD_INFO", "warehouseBin");
                warehouseLoc = localConfig.Read("RUNCARD_INFO", "warehouseLoc");
                partClass = localConfig.Read("RUNCARD_INFO", "partClass");
                machineId = localConfig.Read("RUNCARD_INFO", "machineID");
                opcode = localConfig.Read("RUNCARD_INFO", "opcode");
                seqnum = localConfig.Read("RUNCARD_INFO", "seqnum");

                //Control Adjust
                lblMachine.Text = machineId;
                lblOpcode.Text = opcode;
                lblMessage.Text = "";

                //Temporal Data
                string dBMsg = string.Empty;
                int dBError = 0;

                //Data Base Connection
                DBConnection dB = new DBConnection();
                DataTable dtResult = new DataTable();
                dB.dataBase = "datasource=mlxgumvlptfrd01.molex.com;port=3306;username=ftest;password=Ftest123#;database=runcard_tempflex;";
                dB.query = "SELECT partnum FROM runcard_tempflex.prod_master_config"
                         + " INNER JOIN runcard_tempflex.prod_step_config ON runcard_tempflex.prod_step_config.prr_config_id = runcard_tempflex.prod_master_config.prr_config_id AND runcard_tempflex.prod_step_config.prr_config_rev = runcard_tempflex.prod_master_config.prr_config_rev"
                         + " WHERE status = \"ACTIVE\" AND opcode = \"" + opcode + "\" AND part_class IN ('" + partClass + "');";
                var dBResult = dB.getData(out dBMsg, out dBError);

                if (dBError != 0)
                {
                    //Control Adjust
                    cBoxPartNum.Enabled = false;

                    //Feedback
                    Message message = new Message(dBMsg);
                    message.ShowDialog();
                    return;
                }

                //Fill Data Table
                dBResult.Fill(dtResult);

                foreach (DataRow row in dtResult.Rows)
                    if (!cBoxPartNum.Items.Contains(row.ItemArray[0]))
                        cBoxPartNum.Items.Add(row.ItemArray[0]);
            }
            catch (Exception ex)
            {
                //Control Adjust
                cBoxPartNum.Enabled = false;

                //Feedback
                Message message = new Message("Error al obtener la configuración");
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener la configuración:" + ex.Message + "\n");
            }
        }

        private void cBoxPartNum_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cBoxPartNum.Text != string.Empty)
            {
                try
                {
                    //Clear Save Data
                    cBoxWorkOrder.Items.Clear();

                    //Get Work Orders
                    var getWorkOrders = client.getAvailableWorkOrders(cBoxPartNum.Text, "", out error, out msg);

                    foreach (workOrderItem order in getWorkOrders)
                        if (!cBoxWorkOrder.Items.Contains(order.workorder))
                            cBoxWorkOrder.Items.Add(order.workorder);

                    //Control Adjust
                    cBoxWorkOrder.Enabled = true;
                }
                catch (Exception ex)
                {
                    //Feedback
                    Message message = new Message("Error al obtener las ordenes");
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener las ordenes:" + ex.Message + "\n");
                }
            }
        }

        private void cBoxWorkOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cBoxWorkOrder.Text != string.Empty)
            {
                try
                {
                    //Get BOM
                    getBOM = client.getUnitBOMConsumption(cBoxWorkOrder.Text, seqnum, out error, out msg);

                    if (getBOM.Length == 0)
                    {
                        //Retroalimentación
                        Message message = new Message("La orden actual no cuenta con BOM");
                        message.ShowDialog();

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",La orden actual no cuenta con BOM\n");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    //Retroalimentación
                    Message message = new Message("Error al obtener el BOM");
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener el BOM:" + ex.Message + "\n");
                    return;
                }

                //Contol Adjust
                cBoxWorkOrder.Enabled = false;
                tBoxFinalLabel.Enabled = true;
                cBoxPartNum.Enabled = false;
                btnChange.Enabled = true;
                tBoxFinalLabel.Focus();
            }
        }

        private void tBoxFinalLabel_KeyDown(object sender, KeyEventArgs e)
        {
                if (e.KeyCode == Keys.Enter & tBoxFinalLabel.Text != string.Empty)
                {
                    //Temporal Data
                    int response = 0;
                    textoDespuesDelEspacio = tBoxFinalLabel.Text.Replace(" ", "");

                //Register Unit
                serialRegister(textoDespuesDelEspacio, out response);

                    if (response != 0)
                    {
                        //Control Adjust
                        tBoxFinalLabel.Enabled = false;
                        tBoxTravelLabel.Enabled = true;
                        tBoxTravelLabel.Focus();
                        return;
                    }
                }        
        }

        private void tBoxTravelLabel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter & textoDespuesDelEspacio != string.Empty)
            {
                //Temporal Data
                int response = 0;

                var donnor1 = client.fetchInventoryItems(tBoxTravelLabel.Text, "", "", "", "", "", 0, "", "", out error, out msg);
                float quantity = donnor1[0].qty;
                string status = donnor1[0].status;
                if (opcode == "A102")
                {
                    if(status == "AVAILABLE" | status == "RECEIVED" | status == "COMPLETE")
                    {
                        if (donnor1.Length > 0 && cont <= 1)
                        {

                            Donnorserial[cont] = donnor1[0].serial;
                            DonnorpartNum[cont] = donnor1[0].partnum;
                            Donnorparrev[cont] = donnor1[0].partrev;
                            cont++;
                        }
                    }
                    else
                    {
                        Message message = new Message("La etiqueta Viajera ya fue consumida en otra etiqueta Final");
                        message.ShowDialog();

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",\"La etiqueta Viajera ya fue consumida en otra etiqueta Final:" + msg + "\n");
                    }
                    
                }
                else
                {
                    Donnorserial[0] = donnor1[0].serial;
                    DonnorpartNum[0] = donnor1[0].partnum;
                    Donnorparrev[0] = donnor1[0].partrev;
                }

                if (opcode == "A102")
                {
                    if(cont == 2)
                    {
                        //Transaction Unit
                        serialTransaction(textoDespuesDelEspacio, out response);
                        cont = 0;
                        if (response != 0)
                        {
                            //Control Adjust
                            tBoxTravelLabel.Clear();
                            tBoxTravelLabel.Focus();
                            return;
                        }

                        tBoxFinalLabel.Enabled = true;
                        tBoxTravelLabel.Clear();
                        tBoxFinalLabel.Clear();
                        tBoxFinalLabel.Focus();
                        cont = 0;

                        Array.Clear(Donnorserial, 0, Donnorserial.Length);
                        Array.Clear(DonnorpartNum, 0, DonnorpartNum.Length);
                        Array.Clear(Donnorparrev, 0, Donnorparrev.Length);
                    }
                    else
                    {
                        Message message = new Message("Favor de escanear la segunda etiqueta");
                        message.ShowDialog();
                        tBoxTravelLabel.Clear();
                        tBoxTravelLabel.Focus();
                    }
                }
                else if (opcode == "A106")
                {
                    //Transaction Unit
                    serialTransaction(textoDespuesDelEspacio, out response);
                    cont = 0;
                    if (response != 0)
                    {
                        //Control Adjust
                        tBoxTravelLabel.Clear();
                        tBoxTravelLabel.Focus();
                        return;
                    }

                    //Control Adjust
                    //Donnorserial = null;
                    //DonnorpartNum = null;
                    //Donnorparrev = null;
                    tBoxFinalLabel.Enabled = true;
                    tBoxTravelLabel.Clear();
                    tBoxFinalLabel.Clear();
                    tBoxFinalLabel.Focus();
                    cont = 0;

                    Array.Clear(Donnorserial, 0, Donnorserial.Length);
                    Array.Clear(DonnorpartNum, 0, DonnorpartNum.Length);
                    Array.Clear(Donnorparrev, 0, Donnorparrev.Length);
                }
                tBoxTravelLabel.Clear();
            }
        }
        

        private void serialRegister(string serial, out int response)
        {
            int register = -1;
            response = 0;

            try
            {
                register = client.registerUnitToWorkOrder(cBoxWorkOrder.Text, serial, 1, "", "", "WIP", "PRODUCTION FLOOR", "ftest", out string msg);
                
                if (error != 0)
                {
                    //Retroalimentación
                    Message message = new Message("Error al registrar el serial " + serial);
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al registar el serial " + serial + ":" + msg + "\n");

                    //Response
                    response = -1;
                    return;
                }

                if (msg.Contains("is already registered"))
                {
                    //Retroalimentación
                    Message message = new Message("Serial " + serial + " YA registrado");
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + "Serial " + serial + " YA registrado:" + msg + "\n");

                    //Response
                    response = -1;
                    return;
                }
                
                    response = -1;
                    return;
                
            }
            catch (Exception ex)
            {
                //Feedback
                Message message = new Message("Error al registar el serial " + serial);
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al registar el serial " + serial + ":" + ex.Message + "\n");

                //Response
                response = -1;
            }
        }

        private void serialTransaction(string serial, out int response)
        {
            //Get Status
            InventoryItem[] fetchInv = null;
            string workorder = string.Empty;
            string operation = string.Empty;
            string partnum = string.Empty;
            string partrev = string.Empty;
            string status = string.Empty;
            float quantity = 0;
            int step = 0;

            //Response
            response = 0;

            try
            {
                //Get Unit Status
                fetchInv = client.fetchInventoryItems(serial, "", "", "", "", "", 0, "", "", out error, out msg);
                workorder = fetchInv[0].workorder;
                operation = fetchInv[0].opcode;
                partnum = fetchInv[0].partnum;
                partrev = fetchInv[0].partrev;
                status = fetchInv[0].status;
                step = fetchInv[0].seqnum;
            }
            catch (Exception ex)
            {
                //Feedback
                Message message = new Message("Error al consultar el status del serial " + serial);
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar el status del serial " + serial + ":" + ex.Message + "\n");

                //Response
                response = -1;
                return;
            }

            if (status == "IN QUEUE"  | status == "IN PROGRESS")
            {
                //Transaction Item
                transactionItem transItem = new transactionItem();
                transItem.workorder = cBoxWorkOrder.Text;
                transItem.warehouseloc = warehouseLoc;
                transItem.warehousebin = warehouseBin;
                transItem.username = "ftest";
                transItem.machine_id = machineId;
                transItem.transaction = "MOVE";
                if (opcode == "A102")
                    transItem.opcode = "A102";
                else
                    transItem.opcode = "A106";
                transItem.serial = serial;
                transItem.trans_qty = 1;
                transItem.seqnum = 10;

                var transaction = 0;
                bomItem[] bomData = new bomItem[2];
                bomItem[] bomData2 = new bomItem[1];
                dataItem[] inputData = new dataItem[] { };
                if (opcode == "A102")
                {
                    
                        bomData[0] = new bomItem();
                        bomData[0].item_serial = Donnorserial[0];
                        bomData[0].item_partnum = DonnorpartNum[0];
                        bomData[0].item_partrev = Donnorparrev[0];
                        bomData[0].item_qty = 1;

                        //Load BOM
                        bomData[1] = new bomItem();
                        bomData[1].item_serial = Donnorserial[1];
                        bomData[1].item_partnum = DonnorpartNum[1];
                        bomData[1].item_partrev = Donnorparrev[1];
                        bomData[1].item_qty = 1;

                        cont++;
                    
                    
                }
                else {

                    //Load BOM
                    bomData2[0] = new bomItem();
                    bomData2[0].item_serial = Donnorserial[0];
                    bomData2[0].item_partnum = DonnorpartNum[0];
                    bomData2[0].item_partrev = Donnorparrev[0];
                    bomData2[0].item_qty = 1;
                }

                try
                {
                    
                    if (opcode == "A102")
                        //Transaction
                        transaction = client.transactUnit(transItem, inputData, bomData, out msg);
                    else
                        transaction = client.transactUnit(transItem, inputData, bomData2, out msg);


                    cont = 0;
                    if (!msg.Contains("ADVANCE"))
                    {
                        //Feedback
                        lblMessage.Text = "Pase NO otorgado al serial " + serial;
                        tLayoutMessage.BackColor = Color.Crimson;

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Pase NO otorgado al serial " + serial + ":" + msg + "\n");

                        //Response
                        response = -1;
                        return;
                    }

                    //Feedback
                    lblMessage.Text = "Serial " + serial + " Completado";
                    tLayoutMessage.BackColor = Color.FromArgb(58, 196, 123);

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\Log.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + "," + msg + "\n");
                }
                catch (Exception ex)
                {
                    //Feedback
                    Message message = new Message("Error al dar el pase al serial " + serial);
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al dar el pase al serial " + serial + ":" + ex.Message + "\n");
                    //Response
                    response = -1;
                    return;
                }
            }
            else
            {
                //Get Instructions
                var getInstructions = client.getWorkOrderStepInstructions(workorder, step.ToString(), out error, out msg);

                //Feedback
                lblMessage.Text = "Serial " + serial + " sin flujo, " + status + ":" + getInstructions.opdesc;
                tLayoutMessage.BackColor = Color.Crimson;

                //Response
                response = -1;
            }
        }

        private void lblMessage_TextChanged(object sender, EventArgs e)
        {
            //Timer Start
            timerTextReset.Start();
        }

        private void timerTextReset_Tick(object sender, EventArgs e)
        {
            //Timer Stop
            timerTextReset.Stop();

            //Control Adjust
            tLayoutMessage.BackColor = Color.White;
            lblMessage.Text = string.Empty;
        }


        private void btnChange_Click(object sender, EventArgs e)
        {
            //Control Adjust
            tLayoutMessage.BackColor = Color.White;
            cBoxWorkOrder.SelectedIndex = -1;
            cBoxPartNum.SelectedIndex = -1;
            cBoxWorkOrder.Enabled = false;
            cBoxPartNum.Enabled = true;
            btnChange.Enabled = false;
            lblMessage.Text = "";
            tBoxTravelLabel.Enabled = false;
            tBoxFinalLabel.Enabled = false;
        }
    }
}