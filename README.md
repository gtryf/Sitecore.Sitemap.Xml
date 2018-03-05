# Sitecore.Sitemap.Xml
Sitecore module to automatically generate sitemap.xml files based on CMS content. Tried and tested on Sitecore CMS
7.0, 7.2, 7.5 and Sitecore XP8.

# Installation from source
5 easy steps:
* Download source;
* Copy in the project's /lib directory the Sitecore assemblies mentioned in the readme file;
* Add the Sitemap.Xml project as an existing project in your solution;
* Reference the project in your main Sitecore solution;
* Edit your `web.config` to include the Sitemap.Xml IIS handler (see following section).

# Changes in `web.config`
You need to add the Sitemap.Xml IIS handler in your `web.config` file. This needs to be done in two sections.
## system.webServer
In `system.webServer/handlers`, add the following handler at the end:
```xml
<!-- sitemap.xml: START -->
<add verb="GET" path="sitemap*.xml" 
  type="LD.Sitemap.Xml.SitemapHandler, Sitemap.Xml" 
  name="LD.Sitemap.Xml"/>
<add verb="GET" path="robots.txt"
  type="LD.Sitemap.Xml.RobotsHandler, Sitemap.Xml"
  name="LD.Robots.Txt"/>
<!-- sitemap.xml: END -->
```
## system.web
In `system.web/httpHandlers`, add the following handler at the end:
```xml
<!-- sitemap.xml: START -->
<add verb="GET" path="sitemap*.xml" type="LD.Sitemap.Xml.SitemapHandler, Sitemap.Xml"/>
<add verb="GET" path="robots.txt" type="LD.Sitemap.Xml.RobotsHandler, Sitemap.Xml"/>
<!-- sitemap.xml: END -->
```

# Configuration
The project includes a file called `Sitemap.Xml.config` in its App_Config/Include folder. If you reference this
project in your main Sitecore solution project, then this file should be copied to your project's App_Config/Include
directory. You can use this skeleton configuration to configure the module.

Sitemap.Xml files are generated periodically via a scheduled task (see more information in the section on functionality).
To set the agent interval, please use this configuration section:
```xml
    <scheduling>
      <agent type="LD.Sitemap.Xml.Agents.BuildSitemapAgent,Sitemap.Xml" method="Run" interval="12:00:00" />
    </scheduling>
```
and edit the `interval` attribute. This agent should only run on the Content Management role, therefore please remove it
(or set its interval to zero) on the Content Delivery role.

Furthermore, you can use the `Sitemap.Xml.TempDirectory` setting to specify the 
location on disk where these files are generated. Finally, it is possible to request to build the sitemap.xml files on demand.
To enable or disable this functionality, you can use the `Sitemap.Xml.EnabledBuildOnRequest` setting. 
Please do not enable this on Content Delivery roles.

The configuration file allows for multiple managed websites. Its main node, `<sitemap>`, allows for one or more `<site>`
elements, each one with a `name` required attribute, which must match the name of one of your Sitecore managed
websites, an `embedLanguage` optional attribute (see the section on default behavior for more information),
and a combination of the elements described next. The handler indexes content under and including the
item specified as start item for the corresponding managed website.

#### `site/includeTemplates`
A list of *template* IDs. The handler's output includes managed website items whose template ID matches exactly
one of the IDs specified in this list. Example:

```xml
<site name="demo">
  <includeTemplates>
    <template>{1C637B7D-FFBE-472B-A905-D48BB3B0BC26}</template>
    <template>{E1D0BDDE-18CF-4063-A9A1-8A6C40095BFA}</template>
  </includeTemplates>
</site>
```

#### `site/includeBaseTemplates`
A list of *template* IDs. The handler's output includes managed website items whose template ID, or one of their base
template IDs, matches exactly one of the IDs specified in this list. Example:

```xml
<site name="demo">
  <includeBaseTemplates>
    <template>{7F91172F-22AB-404E-AF61-AD64E0BA1DF4}</template>
  </includeBaseTemplates>
</site>
```
**Caveat: you should make sure that your ContentSearch index indexes all item templates. See the section on default
behavior and extensibility, below.**

#### `site/excludeItems`
A list of *item* IDs. The handler's output excludes managed website items whose item IDs match exactly one of
the IDs specified in this list, even if their template IDs match one of the IDs in the previous configuration
nodes. Example:

```xml
<site name="demo">
  <excludeItems>
    <item>{0790EAB8-466F-46CA-B29C-49D10505AC2E}</item>
  </excludeItems>
</site>
```

# Default behavior and extensibility
At a specified interval, the `LD.Sitemap.Xml.Agents.BuildSitemapAgent` is executed on the CM. This
handler executes the `createSitemapXml` pipeline also defined in `Sitemap.Xml.config` (see section on default behavior) to create the sitemap.xml
files. The pipeline consisits of a single pipeline processor, which provides you with default
behavior, i.e. it knows how to read and make sense of the configuration documented in the previous
section.  If the generated sitemap nodes exceed 10000, or 10MB in size, whichever comes first, then the sitemap
is split into smaller sitemaps and a sitemap_index.xml is generated.

## sitemap.xml
When a request to sitemap.xml is served by the handler, it first checks for the existence of
a sitemap_index.xml and if found, redirects to the index. Otherwise, it serves the siteamp.xml directly.

## robots.txt

Should you choose to also install the `robots.txt` handler, requests to robots.txt serve a text file as follows:
```
User-agent: *
Allow: /
```
followed by the sitemaps, i.e. either:
```
http://www.yoursite.com/sitemap.xml
```
or
```
Sitemap: http://www.yoursite.com/sitemap_index.xml
Sitemap: http://www.yoursite.com/sitemap_segment_1.xml
Sitemap: http://www.yoursite.com/sitemap_segment_2.xml
.
.
.
Sitemap: http://www.yoursite.com/sitemap_segment_n.xml
```
depending on whether a sitemap_index.xml was generated or not.

If you need more control over your robots.txt file, you can choose not to install
this handler.

## Default behavior
To produce its output, the `createSitemapXml` pipeline uses ContentSearch to fetch the items that conform to the template/item
restrictions you have configured. The pipeline processor is defined in `Sitemap.Xml.config` as follows:

```xml
<pipelines>
  <createSitemapXml>
    <processor type="LD.Sitemap.Xml.Pipelines.DefaultSitemapXmlProcessor, Sitemap.Xml">
      <param desc="The index to use (leave empty to use default index)"></param>
    </processor>
  </createSitemapXml>
</pipelines>
```
The parameter to the pipeline processor specifies the ContentSearch index to use. If you leave it blank (as in
the example configuration), it will default to the index resolved by the `contentSearch.getContextIndex`
pipeline for the managed website's start item. *In any case, should you choose to index items by using restrictions
on their base template, you should configure your ContentSearch indexes such that they include all templates
(in the `_temaplates` index field)*, i.e.:

```xml
<fields hint="raw:AddComputedIndexField">
  <!-- other computed index fields here... -->
  <field fieldName="_templates" storageType="yes" indexType="untokenized">
    Sitecore.ContentSearch.ComputedFields.AllTemplates, Sitecore.ContentSearch
  </field>
</fields>
```

Each `/configuration/sitecore/site` element may have an optional `embedLanguage` boolean attribute. If this attribute
is `true` (which is the default value), then the processor runs for all installed languages, and produces sitemap nodes
which always have the language embedded, e.g. for the same page on a UK/French website:
```xml
<loc>http://<domain>.com/en-gb/page1</loc>
<loc>http://<domain>.com/fr-fr/page1</loc>
```
If, however, you set this attribute to `false`, then your website must use its domain to distinguish the language and
never embeds it into the sitemap nodes, so if for the above website you request `http://<domain>.co.uk/sitemap.xml`
you will get:
```xml
<loc>http://<domain>.co.uk/page1</loc>
```
while `http://<domain>.fr/sitemap.xml` should give you:
```xml
<loc>http://<domain>.fr/page1</loc>
```

## Extending the pipeline
You may choose to customize the pipeline by adding to it one or more pipeline processors that you write, or
indeed replace the default processor entirely. This could be because you do not wish to use ContentSearch, or
because you may want to index items from sources other than the Sitecore content tree. If you choose to write
your own pipeline processor, it should inherit from `LD.Sitemap.Xml.Pipelines.CreateSitemapXmlProcessor`
(in the `Sitemap.Xml` assembly) and override the `Process(CreateSitemapXmlArgs)` method. Once you inherit this
class, you will have the `Configuration` property available to you, which is a `Dictionary<string, SiteDefinition>`
containing the configuration for each managed template (i.e. three properties: `IncludedTemplates`;
`IncludedBaseTemplates`; and `ExcludedItems`, which are all `List<string>` containing the configured IDs).

In your `Process()` method, you are required to add to the `args.Nodes` list, which is a list of `UrlDefinition`.
`UrlDefinition`s consist of a `string` that corresponds to a URL, and a `DateTime` corresponding to the last
modified date/time. The elements you add to this list are added to the final sitemap.xml output.
