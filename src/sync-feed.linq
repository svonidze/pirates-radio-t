<Query Kind="Program">
  <NuGetReference>HtmlAgilityPack</NuGetReference>
  <NuGetReference>StringFormat</NuGetReference>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>StringFormat</Namespace>
</Query>

string path = @"z:\others\pirates-radio-t";

string feedXmlFilePath = @"out\feed.xml";

string feedXmlTemplateFilePath = @"src\templates\feed.template.xml";
string feedItemXmlTemplateFilePath = @"src\templates\feed-item.template.xml";

string allPostsUrl = @"http://pirates.radio-t.com/posts/";
string audioUrlFormat = @"http://cdn.radio-t.com/rt{0}post.mp3";
string chatUrlFormat = @"http://chat.radio-t.com/logs/radio-t-{0}.html";

string xPathToArticles = @"/html/body/div[1]/div/div/article/div";

Regex numberRegex = new Regex(@"(\d+)\/?$");

void Main()
{
	var doc = new HtmlWeb().Load(allPostsUrl);

	var items = (from node in doc.DocumentNode.SelectNodes(xPathToArticles).Descendants("article")
		let h1 = node.SelectSingleNode("h1")
		let title = h1.InnerText
		let url = h1.SelectSingleNode("a").Attributes["href"].Value
		let numder = ExtractNumber(title) ?? ExtractNumber(url)
		let time = node.SelectSingleNode("time").Attributes["datetime"].Value
		select new
		{
			Title = title,
			Url = url,
			PubDate = FixDateTime(time),
			AudioUrl = string.Format(audioUrlFormat, numder),
			ChatUrl = string.Format(chatUrlFormat, numder),
			Numder = numder
		})
	.ToArray()
	.Dump();
	
	
	var itemPattern = File.ReadAllText(Path.Combine(path, feedItemXmlTemplateFilePath));
	itemPattern.Dump();
	var itemsString = items
		.Where(i => i.Numder.HasValue)
		.OrderByDescending(i => i.Numder)
		.Select(item => StringFormat.TokenStringFormat.Format(itemPattern, item))
		.ToArray();
	itemsString.Dump();
	
	var pattern = File.ReadAllText(Path.Combine(path, feedXmlTemplateFilePath));
	File.WriteAllText(Path.Combine(path, feedXmlFilePath), string.Format(pattern, string.Join(Environment.NewLine, itemsString)));
	//using(var patternStream = File.OpenRead(feedXmlTemplateFilePath))
	//using(var feedStream = File.OpenWrite(feedXmlFilePath))	
}

int? ExtractNumber(string input)
{
	// OK		После РТ 259 http://pirates.radio-t.com/p/2011/10/15/podcast-259/ 
	// OK		После РТ 655 http://pirates.radio-t.com/p/2019/06/22/podcast/ 
	// NotOk	После РТ http://pirates.radio-t.com/p/2011/10/09/podcast/ 
	// NotOk	После РТ Сайт пиратов переехал http://pirates.radio-t.com/p/2011/09/24/others/ 
	var numberString = numberRegex.Match(input).Value;
	if(int.TryParse(numberString, out int number))
		return number;
	return null;
}

string FixDateTime(string input)
{
	//2020-05-02T57:29:29&#43;00:00
	//return input.Replace("&#43;", "+");
	return input.Split("T").First();
}