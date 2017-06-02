
function buildGraph() {

    var links = [
      { source: "Microsoft", target: "Amazon", type: "predecessor" },
      { source: "Microsoft", target: "HTC", type: "predecessor" },
      { source: "Samsung", target: "Apple", type: "predecessor" },
      { source: "Motorola", target: "Apple", type: "predecessor" },
      { source: "Nokia", target: "Apple", type: "predecessor" },
      { source: "HTC", target: "Apple", type: "predecessor" },
      { source: "Kodak", target: "Apple", type: "predecessor" },
      { source: "Microsoft", target: "Barnes & Noble", type: "predecessor" },
      { source: "Microsoft", target: "Foxconn", type: "predecessor" },
      { source: "Oracle", target: "Google", type: "predecessor" },
      { source: "Apple", target: "HTC", type: "predecessor" },
      { source: "Microsoft", target: "Inventec", type: "predecessor" },
      { source: "Samsung", target: "Kodak", type: "predecessor" },
      { source: "LG", target: "Kodak", type: "predecessor" },
      { source: "RIM", target: "Kodak", type: "predecessor" },
      { source: "Sony", target: "LG", type: "predecessor" },
      { source: "Kodak", target: "LG", type: "predecessor" },
      { source: "Apple", target: "Nokia", type: "predecessor" },
      { source: "Qualcomm", target: "Nokia", type: "predecessor" },
      { source: "Apple", target: "Motorola", type: "predecessor" },
      { source: "Microsoft", target: "Motorola", type: "predecessor" },
      { source: "Motorola", target: "Microsoft", type: "predecessor" },
      { source: "Huawei", target: "ZTE", type: "predecessor" },
    { source: "Ericsson", target: "ZTE", type: "predecessor" },
      { source: "Kodak", target: "Samsung", type: "predecessor" },
      { source: "Apple", target: "Samsung", type: "predecessor" },
      { source: "Kodak", target: "RIM", type: "predecessor" },
      { source: "Nokia", target: "Qualcomm", type: "predecessor" }
    ];


    var nodes = {};

    // Compute the distinct nodes from the links.
    links.forEach(function (link) {
        link.source = nodes[link.source] || (nodes[link.source] = { name: link.source });
        link.target = nodes[link.target] || (nodes[link.target] = { name: link.target });
    });

    var width = 500,
        height = 350;

    var force = d3.layout.force()
        .nodes(d3.values(nodes))
        .links(links)
        .size([width, height])
        .linkDistance(60)
        .charge(-300)
        .on("tick", tick)
        .start();

    var svg = d3.select("#my_svg").append("svg")
        .attr("width", width)
        .attr("height", height)
        .attr("style", "margin: auto;");

    // Per-type markers, as they don't inherit styles.
    svg.append("defs").selectAll("marker")
        .data(["predecessor"])
      .enter().append("marker")
        .attr("id", function (d) { return d; })
        .attr("viewBox", "0 -5 10 10")
        .attr("refX", 15)
        .attr("refY", -1.5)
        .attr("markerWidth", 6)
        .attr("markerHeight", 6)
        .attr("orient", "auto")
      .append("path")
        .attr("d", "M0,-5L10,0L0,5");

    var path = svg.append("g").selectAll("path")
        .data(force.links())
      .enter().append("path")
        .attr("class", function (d) { return "link " + d.type; })
        .attr("marker-end", function (d) { return "url(#" + d.type + ")"; });

    var circle = svg.append("g").selectAll("circle")
        .data(force.nodes())
      .enter().append("circle")
        .attr("r", 6)
        .call(force.drag);

    var text = svg.append("g").selectAll("text")
        .data(force.nodes())
      .enter().append("text")
        .attr("x", 8)
        .attr("y", ".31em")
        .text(function (d) { return d.name; });


    // Use elliptical arc path segments to doubly-encode directionality.
    function tick() {
        path.attr("d", linkArc);
        circle.attr("transform", transform);
        text.attr("transform", transform);
    }

    function linkArc(d) {
        var dx = d.target.x - d.source.x,
            dy = d.target.y - d.source.y,
            dr = Math.sqrt(dx * dx + dy * dy);
        return "M" + d.source.x + "," + d.source.y + "A" + dr + "," + dr + " 0 0,1 " + d.target.x + "," + d.target.y;
    }

    function transform(d) {
        return "translate(" + d.x + "," + d.y + ")";
    }
}