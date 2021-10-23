<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="html" omit-xml-declaration="yes" encoding="UTF-8" indent="yes" />
    <xsl:template match="/">
        <xsl:apply-templates  mode='item'/>
    </xsl:template>
    <xsl:template match='item' mode='item'>
        <tr class="itemRow">
            <td class="idCell hide"><xsl:value-of select="id" /></td>
            <td class="quantityCell"><xsl:value-of select="quantity" /></td>
            <td class="itemCell"><xsl:value-of select="name" /></td>
            <td class="unitpriceCell hide"><xsl:value-of select="unitprice" /></td>
            <td class="printGroupCell hide"><xsl:value-of select="printgroup" /></td>
            <td class="totalpriceCell"><xsl:value-of select="totalprice" /></td>
            <td class="coverCell hide"><xsl:value-of select="cover" /></td>
            <td class="clerkCell"><xsl:value-of select="../@clerk" /></td>
            <td class="originalQtyCell hide"><xsl:value-of select="quantity" /></td>
            <td class="categoryCell hide"><xsl:value-of select="category" /></td>
            <td class="departmentCell hide"><xsl:value-of select="department" /></td>
            <td class="originalTotalPriceCell hide"><xsl:value-of select="originaltotalprice" /></td>
        </tr>
        <xsl:apply-templates select="instruction" mode='instruction'/>
    </xsl:template>
    <xsl:template match='instruction' mode='instruction'>
        <tr class="instructionRow">
            <td class="idCell hide"><xsl:value-of select="id" /></td>
            <td class="quantityCell invisibleText">1</td>
            <td class="itemCell"><xsl:value-of select="name" /></td>
            <td class="unitpriceCell hide"><xsl:value-of select="unitprice" /></td>
            <td class="printGroupCell hide"></td>
            <xsl:if test="not(number(unitprice) = '0')">
                <td class="totalpriceCell"><xsl:value-of select="unitprice" /></td>
            </xsl:if>
            <xsl:if test="number(unitprice) = '0'">
                <td class="totalpriceCell invisibleText"><xsl:value-of select="unitprice" /></td>
            </xsl:if>
            <td class="coverCell hide"><xsl:value-of select="../cover" /></td>
            <td class="clerkCell"><xsl:value-of select="../../@clerk" /></td>
            <td class="originalQtyCell hide">1</td>
            <td class="categoryCell hide"><xsl:value-of select="../category" /></td>
            <td class="departmentCell hide"><xsl:value-of select="../department" /></td>
            <td class="originalTotalPriceCell hide"><xsl:value-of select="originaltotalprice" /></td>
        </tr>
    </xsl:template>
</xsl:stylesheet>