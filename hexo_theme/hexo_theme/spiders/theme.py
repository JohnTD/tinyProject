from scrapy import Spider, Request
from scrapy.selector import HtmlXPathSelector
from scrapy.utils.url import urljoin_rfc
from hexo_theme.items import ThemeItem

import sys
sys.stdout = open('output.txt', 'w')

class ThemeSpider(Spider):
    name = 'theme'
    allowed_domains = ['github.com']
    start_urls = ['https://github.com/hexojs/hexo/wiki/Themes']

    def parse(self, response):
        hxs  = HtmlXPathSelector(response)
        urls = hxs.xpath('//div[@class="markdown-body"]/ul/li/strong/a/@href').extract()

        for url in urls:
            yield Request(url, callback = self.parse_theme)

    def parse_theme(self, response):
        item  = ThemeItem()
        hxs   = HtmlXPathSelector(response)
        root  = hxs.xpath('//div[@class="clearfix"]')
        item['star'] = root.xpath('./ul/li[2]/a[2]/text()').extract()[0].split()
        father= root.xpath('./h1/span[@class="author"]/a/span/text()').extract()
        child = root.xpath('./h1/strong/a/text()').extract()
        item['name'] = '/'.join(father+child)
        rpath = root.xpath('./h1/strong/a/@href').extract()[0]
        item['url'] = urljoin_rfc('https://github.com', rpath)

        return item
