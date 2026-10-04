"""Equivalence to the established pilot footprint, including canvas borders."""
import unittest
import numpy as np
from charkit import imgops as reference
import processing_stencils as fast


class ExactStencilTests(unittest.TestCase):
    def test_binary_footprint_and_border_equivalence(self):
        rng=np.random.default_rng(612)
        for shape in [(1,1),(3,7),(47,59)]:
            for density in [0.,.05,.5,.95,1.]:
                mask=rng.random(shape)<density
                for radius in range(0,9):
                    # Original shifts assume r smaller than each side. For
                    # one-pixel canvases the no-op itself is the contract.
                    if radius>=min(shape):
                        continue
                    np.testing.assert_array_equal(fast.dilate(mask,radius),reference.dilate(mask,radius))
                    np.testing.assert_array_equal(fast.erode(mask,radius),reference.erode(mask,radius))

    def test_valid_colour_extrema_equivalence(self):
        rng=np.random.default_rng(901)
        values=rng.normal(size=(43,57)).astype(np.float32)
        for density in [0.,.08,.5,1.]:
            valid=rng.random(values.shape)<density
            for radius in range(0,9):
                np.testing.assert_array_equal(fast.local_max(values,valid,radius),reference.local_max(values,valid,radius))

    def test_colour_bleed_crop_equivalence(self):
        rng=np.random.default_rng(182)
        rgb=rng.uniform(0,255,(47,59,3)).astype(np.float32)
        for rectangle in [(20,25,22,29),(0,4,0,4),(0,47,0,59)]:
            valid=np.zeros((47,59),bool)
            y0,y1,x0,x1=rectangle
            valid[y0:y1,x0:x1]=True
            for steps in [0,1,3,8]:
                expected=reference.bleed(rgb,valid,steps)
                actual=fast.bleed(rgb,valid,steps)
                for a,b in zip(expected,actual):
                    np.testing.assert_array_equal(a,b)


if __name__=='__main__':
    unittest.main()
