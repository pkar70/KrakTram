<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
  <xsl:template match='/'>
    <html>
      <body>
        <ul>
          <xsl:apply-templates select='root/item'>
            <xsl:sort data-type='number' select='@odl' />
          </xsl:apply-templates>
        </ul>
      </body>
    </html>
  </xsl:template>

  <xsl:template match='root/item'>
    <li>
      <b>
        <xsl:value-of select='@name'/>
      </b>, <xsl:value-of select='@odl'/> m
    </li>
  </xsl:template>
</xsl:stylesheet>
