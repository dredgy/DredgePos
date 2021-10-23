<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" omit-xml-declaration="yes" encoding="UTF-8" indent="yes"  />
<!-- root - kick things off -->
    <xsl:template match="/">
        <order clerk="{table/@data-clerk}">
            <xsl:apply-templates select='table/tbody/tr[@class = "itemRow"]' mode='item'/>
        </order>
    </xsl:template>

    <!-- iteration item - item -->
    <xsl:template match='tr' mode='item'>
        <item type="item" split="0">
            <id><xsl:value-of select="td[contains(@class, 'idCell')]" /></id>
            <quantity><xsl:value-of select="td[contains(@class,'qtyCell')]" /></quantity>
            <name><xsl:value-of select="td[contains(@class, 'itemCell')]/a[contains(@class, 'itemText')]" /></name>
            <unitprice><xsl:value-of select="td[contains(@class,'unitPriceCell')]" /> </unitprice>
            <totalprice><xsl:value-of select="td[contains(@class,'totalPriceCell')]" /> </totalprice>
            <printgroup><xsl:value-of select="td[contains(@class,'printGroupCell')]" /></printgroup>
            <cover><xsl:value-of select="td[contains(@class,'coverCell')]" /></cover>
            <category><xsl:value-of select="td[contains(@class,'categoryCell')]" /></category>
            <department><xsl:value-of select="td[contains(@class,'departmentCell')]" /></department>
             <originalqty><xsl:value-of select="td[contains(@class,'qtyCell')]" /></originalqty>
              <originaltotalprice><xsl:value-of select="td[contains(@class,'totalPriceCell')]" /></originaltotalprice>
            <xsl:apply-templates select='following-sibling::tr[@class = "instructionRow" and preceding-sibling::tr[@class = "itemRow"][1] = current()]' mode='instruction'/>
        </item>
    </xsl:template>

    <!-- iteration item - instruction -->
    <xsl:template match='tr' mode='instruction'>
        <instruction split="0">
            <id><xsl:value-of select="td[contains(@class,'idCell')]" /></id>
            <name><xsl:value-of select="td[contains(@class, 'itemCell')]/a[contains(@class, 'itemText')]" /> </name>
            <unitprice><xsl:value-of select="td[contains(@class,'unitPriceCell')]" /> </unitprice>
            <originalqty><xsl:value-of select="td[contains(@class,'qtyCell')]" /></originalqty>
             <originaltotalprice><xsl:value-of select="td[contains(@class,'totalPriceCell')]" /></originaltotalprice>
        </instruction>
    </xsl:template>
</xsl:stylesheet>