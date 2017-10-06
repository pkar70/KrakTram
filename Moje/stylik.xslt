<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
  <xsl:template match='/'>
    <HTML>
      <BODY>
		  <table border='1'>
 <xsl:apply-templates select='root/item'>
    <xsl:sort select='@line'/>
   </xsl:apply-templates>
		  </table>
      </BODY>
    </HTML>
  </xsl:template>


  <xsl:template match='root/item'>
	  <tr>
		  <td rowspan='2'>
			  <font size='+2'> <b>  <xsl:value-of select='@line'/>  </b>  </font>  <br/>
				<font size='-1'> <xsl:value-of select='@typ'/></font>
		  </td>

		<td><b> <xsl:value-of select='@dir'/> </b></td>

		<td rowspan='2'> <font size='+1'> <xsl:value-of select='@time'/> </font>  </td>
	  </tr>
	  <tr>
		  <td>
			  <font size='-1'>  <xsl:value-of select='@stop'/> <small>
				  (  <xsl:value-of select='@odl'/> )</small>  </font>
		  </td>
	  </tr>

  </xsl:template>
</xsl:stylesheet>