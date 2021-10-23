<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" omit-xml-declaration="yes" />
<xsl:template match="/">
    <xsl:for-each select="table/tbody/tr">\n<xsl:value-of select="td[2]" />x <xsl:value-of select="td[3]" />\n</xsl:for-each>
</xsl:template>
</xsl:stylesheet>