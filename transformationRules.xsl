<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:template match="catalog">

<HTML>

<BODY>

<H2>Список дисків</H2> 

<TABLE BORDER="2">

<THEAD>

<TR>

<TH>

<b>Назва композиції</b>

</TH>

<TH>

<b>Виконавець</b>

</TH>

<TH>

<b>Країна</b>

</TH>

<TH>

<b>Компанія-продюсер</b>

</TH>

<TH>

<b>Ціна</b>

</TH>

<TH>

<b>Рік випуску</b>

</TH>

</TR>

</THEAD>

<TBODY>

<xsl:for-each select="/*/cd">

<TR>

<TD>

<b><xsl:value-of select="./title" /></b>

</TD>

<TD>

<b><xsl:value-of select="./artist" /></b>

</TD>

<TD>

<b><xsl:value-of select="./country" /></b>

</TD>

<TD>

<b><xsl:value-of select="./company" /></b>

</TD>

<TD>

<b><xsl:value-of select="./price" /></b>

</TD>

<TD>

<b><xsl:value-of select="./year" /></b>


</TD>

</TR>

</xsl:for-each>

</TBODY>

</TABLE>

</BODY>

</HTML>

</xsl:template>
</xsl:stylesheet>