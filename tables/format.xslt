<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
  <html>
  <body>
  Table <xsl:value-of select="//@number"/>
    <table>
      <xsl:for-each select="table/order">
        <xsl:for-each select="item">
            <tr>
              <td><xsl:value-of select="name"/></td>
              <td><xsl:value-of select="qty"/> @ $<xsl:value-of select="price"/></td>
              <td>Total price: $<xsl:value-of select="qty * price"/></td>
              <td>Ordered by Clerk <xsl:value-of select="../@clerk"/></td>
              <td>Ordered at <xsl:value-of select="../@time"/></td>
                <td><xsl:value-of select="printGroup"/></td>
            </tr>
        </xsl:for-each>
      </xsl:for-each>
    </table>
  </body>
  </html>
</xsl:template>

</xsl:stylesheet>