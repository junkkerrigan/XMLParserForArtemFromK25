<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:template match="catalog">

<HTML>

<BODY>

<H2>Книги</H2> 

<TABLE BORDER="2">

<THEAD>

<TR>

<TH>

<b>Назва</b>

</TH>

<TH>

<b>Автор</b>

</TH>

<TH>

<b>Опис</b>

</TH>

<TH>

<b>Жанр</b>

</TH>

<TH>

<b>Рік випуску</b>

</TH>

<TH>

<b>Ціна</b>

</TH>

</TR>

</THEAD>

<TBODY>

<xsl:for-each select="/*/book">

<TR>

<TD>

<b><xsl:value-of select="./title" /></b>

</TD>

<TD>

<b><xsl:value-of select="./author" /></b>

</TD>

<TD>

<b><xsl:value-of select="./description" /></b>

</TD>

<TD>

<b><xsl:value-of select="./genre" /></b>

</TD>

<TD>

<b><xsl:value-of select="./publishYear" /></b>

</TD>

<TD>

<b><xsl:value-of select="./price" /></b>


</TD>

</TR>

</xsl:for-each>

</TBODY>

</TABLE>

</BODY>

</HTML>

</xsl:template>
</xsl:stylesheet>