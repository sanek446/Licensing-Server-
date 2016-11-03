<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Registration.aspx.cs" Inherits="WebApplication2.Registration"  %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Registration</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>                

<h5>Serial number</h5>
        <asp:textbox id="serialnumber" runat="server" />      

<h5>E-mail</h5>
        <asp:textbox id="email" runat="server" />
        
<h5>Password</h5>
        <asp:TextBox id="password" TextMode="Password" runat="server" />        

<h5>Confirmation password</h5>
        <asp:TextBox id="confpassword" TextMode="Password" runat="server" />               
            
        <br/>
        <br/>
<asp:Label id="Label1" Text="" runat="server" />
        <br/>
        <br/>

        <asp:Button id="Register"
           Text="Register"
           OnClick="RegisterBtn_Click" 
           runat="server"/>

            <asp:Button id="UnRegister"
           Text="Unregister"
           OnClick="UnRegisterBtn_Click" 
           runat="server"/>

        <br/>
        <br/>
                
            <asp:Button id="ForgotPassword"
           Text="Forgot password?"
           OnClick="ForgotPasswordBtn_Click" 
           runat="server"/>

        <br/>
        <br/>        

    </div>

    </form>

      <footer class="app-footer">
        <p class="container" style="padding-top:15px">&copy; 2016 - RFQ2Go Online Portal</p>
    </footer>
    
</body>
</html>
